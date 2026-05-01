// supabase/functions/thief-guess/index.ts
// Edge Function: Handle the Thief guessing phase at end of match.

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { getAdminClient, badRequest, ok, internalError } from "../_shared/supabase-admin.ts";

interface ThiefGuessRequest {
  match_id: string;
  caller_id: string;
  guessed_player_id: string; // The player the caller thinks holds the Thief
}

serve(async (req: Request) => {
  if (req.method !== "POST") return badRequest("Only POST");

  let body: ThiefGuessRequest;
  try {
    body = await req.json();
  } catch {
    return badRequest("Invalid JSON");
  }

  const { match_id, caller_id, guessed_player_id } = body;
  if (!match_id || !caller_id || !guessed_player_id) {
    return badRequest("match_id, caller_id, and guessed_player_id are required");
  }

  const supabase = getAdminClient();

  try {
    // Fetch match
    const { data: match, error: mErr } = await supabase
      .from("matches")
      .select("screw_caller_id, status")
      .eq("id", match_id)
      .single();

    if (mErr) return internalError(mErr.message);
    if (match.screw_caller_id !== caller_id) return badRequest("Only the Screw caller can guess");

    // Fetch players
    const { data: players, error: pErr } = await supabase
      .from("match_players")
      .select("user_id, has_thief, final_score")
      .eq("match_id", match_id);

    if (pErr) return internalError(pErr.message);

    const thiefHolder = players!.find((p: any) => p.has_thief);
    if (!thiefHolder) {
      return ok({ success: true, reason: "no_thief_in_play" });
    }

    const guessCorrect = thiefHolder.user_id === guessed_player_id;
    let result: string;

    if (guessCorrect) {
      // Thief neutralized – holder absorbs their own penalty
      result = "thief_neutralized";

      await supabase.channel(`match:${match_id}`).send({
        type: "broadcast",
        event: "thief_guess_result",
        payload: {
          correct: true,
          thief_holder_id: thiefHolder.user_id,
          result,
        },
      });

      // Mission progress for catching the thief
      await supabase.rpc("increment_mission_progress", {
        p_user_id: caller_id,
        p_mission_key: "thief",
      });
    } else {
      // Failed guess – swap final scores between caller and thief holder
      const callerPlayer = players!.find((p: any) => p.user_id === caller_id);
      const callerScore = callerPlayer?.final_score ?? 0;
      const holderScore = thiefHolder.final_score ?? 0;

      await supabase
        .from("match_players")
        .update({ final_score: holderScore })
        .eq("match_id", match_id)
        .eq("user_id", caller_id);

      await supabase
        .from("match_players")
        .update({ final_score: callerScore })
        .eq("match_id", match_id)
        .eq("user_id", thiefHolder.user_id);

      result = "scores_swapped";

      await supabase.channel(`match:${match_id}`).send({
        type: "broadcast",
        event: "thief_guess_result",
        payload: {
          correct: false,
          thief_holder_id: thiefHolder.user_id,
          caller_new_score: holderScore,
          holder_new_score: callerScore,
          result,
        },
      });
    }

    return ok({ success: true, guess_correct: guessCorrect, result });
  } catch (err) {
    console.error("[thief-guess] Error:", err);
    return internalError("Unexpected error");
  }
});
