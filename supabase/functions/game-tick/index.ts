// supabase/functions/game-tick/index.ts
// Edge Function: Heartbeat called by pg_cron every 5 seconds.
// Expires turn timers and advances turn state if a player runs out of time.

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { getAdminClient, ok, internalError } from "../_shared/supabase-admin.ts";

serve(async (_req: Request) => {
  const supabase = getAdminClient();

  try {
    // Find all in-progress matches whose turn has expired
    const now = new Date().toISOString();

    const { data: expiredMatches, error } = await supabase
      .from("matches")
      .select("id, current_turn_index, turn_started_at, turn_timeout_secs")
      .eq("status", "in_progress")
      .not("turn_started_at", "is", null);

    if (error) return internalError(error.message);

    for (const match of expiredMatches ?? []) {
      const turnStarted = new Date(match.turn_started_at);
      const deadlineMs = turnStarted.getTime() + match.turn_timeout_secs * 1000;
      const nowMs = Date.now();

      if (nowMs >= deadlineMs) {
        // Turn has expired – fetch players to determine next seat
        const { data: players } = await supabase
          .from("match_players")
          .select("seat_index, user_id, is_connected, is_screwed")
          .eq("match_id", match.id)
          .order("seat_index", { ascending: true });

        if (!players?.length) continue;

        const totalSeats = players.length;
        let nextIndex = (match.current_turn_index + 1) % totalSeats;

        // Skip screwed or disconnected seats
        let attempts = 0;
        while (
          attempts < totalSeats &&
          (players[nextIndex].is_screwed || !players[nextIndex].is_connected)
        ) {
          nextIndex = (nextIndex + 1) % totalSeats;
          attempts++;
        }

        // Update match with next turn
        await supabase
          .from("matches")
          .update({
            current_turn_index: nextIndex,
            turn_started_at: now,
          })
          .eq("id", match.id);

        // Broadcast turn change
        await supabase.channel(`match:${match.id}`).send({
          type: "broadcast",
          event: "turn_changed",
          payload: {
            previous_seat: match.current_turn_index,
            current_seat: nextIndex,
            reason: "timeout",
          },
        });
      }
    }

    return ok({ processed: expiredMatches?.length ?? 0 });
  } catch (err) {
    console.error("[game-tick] Error:", err);
    return internalError("Unexpected error in game-tick");
  }
});
