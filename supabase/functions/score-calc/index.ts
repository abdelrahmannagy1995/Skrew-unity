// supabase/functions/score-calc/index.ts
// Edge Function: Calculate final scores, apply Screw penalty x2, handle Thief swap.

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { getAdminClient, badRequest, ok, internalError } from "../_shared/supabase-admin.ts";
import { CardType, SpecialCardId } from "../_shared/card-definitions.ts";

interface ScoreCalcRequest {
  match_id: string;
}

serve(async (req: Request) => {
  if (req.method !== "POST") return badRequest("Only POST");

  let body: ScoreCalcRequest;
  try {
    body = await req.json();
  } catch {
    return badRequest("Invalid JSON");
  }

  const { match_id } = body;
  if (!match_id) return badRequest("match_id is required");

  const supabase = getAdminClient();

  try {
    // Fetch match
    const { data: match, error: mErr } = await supabase
      .from("matches")
      .select("screw_caller_id, game_mode, status")
      .eq("id", match_id)
      .single();

    if (mErr) return internalError(mErr.message);
    if (match.status !== "endgame") return badRequest("Match is not in endgame phase");

    // Fetch all players with their hands
    const { data: players, error: pErr } = await supabase
      .from("match_players")
      .select("user_id, hand, seat_index, has_thief")
      .eq("match_id", match_id)
      .order("seat_index", { ascending: true });

    if (pErr) return internalError(pErr.message);

    // Calculate raw scores
    const scores: Record<string, number> = {};
    let thiefHolderId: string | null = null;

    for (const player of players!) {
      let total = 0;
      const hand: any[] = player.hand ?? [];

      for (const card of hand) {
        if (card.type === CardType.Command) {
          total += 10; // command card endgame penalty
        } else if (card.specialId === SpecialCardId.Thief) {
          thiefHolderId = player.user_id;
          // Thief itself doesn't add points to holder
        } else {
          total += card.value ?? 0;
        }
      }

      scores[player.user_id] = total;
    }

    const callerId = match.screw_caller_id;
    const callerScore = scores[callerId];
    const callerHand: any[] = players!.find((p: any) => p.user_id === callerId)?.hand ?? [];
    const callerHasEmptyHand = callerHand.length === 0;

    // Determine if caller is the strict lowest
    const otherScores = Object.entries(scores)
      .filter(([uid]) => uid !== callerId)
      .map(([, s]) => s);

    const callerWon = otherScores.every((s) => callerScore < s);

    // Apply Thief logic (only in 'general' or 'thief' mode)
    // Thief power is bypassed if caller had obligatory screw (empty hand)
    let thiefSwapOccurred = false;
    if (
      thiefHolderId &&
      match.game_mode !== "classic" &&
      match.game_mode !== "doubles" &&
      !callerHasEmptyHand
    ) {
      // The Thief swap will be resolved by the thief-guess function.
      // Here we just flag it for the response.
      thiefSwapOccurred = true;
    }

    // Apply x2 penalty if caller did NOT win
    if (!callerWon && !callerHasEmptyHand) {
      scores[callerId] = callerScore * 2;
    }

    // Persist final scores and update match status
    for (const player of players!) {
      await supabase
        .from("match_players")
        .update({ final_score: scores[player.user_id] })
        .eq("match_id", match_id)
        .eq("user_id", player.user_id);
    }

    // Determine winner (lowest score)
    const sortedPlayers = Object.entries(scores).sort(([, a], [, b]) => a - b);
    const [winnerId] = sortedPlayers[0];

    // Update match as completed
    await supabase
      .from("matches")
      .update({ status: "completed" })
      .eq("id", match_id);

    // Update ELO ratings
    await updateEloRatings(supabase, players!, winnerId, scores);

    // Broadcast reveal
    await supabase.channel(`match:${match_id}`).send({
      type: "broadcast",
      event: "match_completed",
      payload: {
        scores,
        winner_id: winnerId,
        caller_id: callerId,
        caller_penalized: !callerWon && !callerHasEmptyHand,
        thief_swap_pending: thiefSwapOccurred,
        thief_holder_id: thiefSwapOccurred ? thiefHolderId : null,
      },
    });

    return ok({
      scores,
      winner_id: winnerId,
      thief_swap_pending: thiefSwapOccurred,
    });
  } catch (err) {
    console.error("[score-calc] Error:", err);
    return internalError("Unexpected error");
  }
});

async function updateEloRatings(
  supabase: any,
  players: any[],
  winnerId: string,
  scores: Record<string, number>
): Promise<void> {
  const K = 32;
  const playerElos: Record<string, number> = {};

  // Fetch current ELO ratings
  const userIds = players.map((p: any) => p.user_id);
  const { data: users } = await supabase
    .from("users")
    .select("id, elo_rating")
    .in("id", userIds);

  for (const u of users ?? []) {
    playerElos[u.id] = u.elo_rating;
  }

  // Simple ELO update: winner gains K points averaged against all opponents
  const opponentIds = userIds.filter((id: string) => id !== winnerId);
  const winnerElo = playerElos[winnerId] ?? 1000;

  for (const oppId of opponentIds) {
    const oppElo = playerElos[oppId] ?? 1000;
    const expectedWin = 1 / (1 + Math.pow(10, (oppElo - winnerElo) / 400));
    const delta = Math.round(K * (1 - expectedWin));
    playerElos[winnerId] = (playerElos[winnerId] ?? 1000) + delta;
    playerElos[oppId] = Math.max(0, (playerElos[oppId] ?? 1000) - delta);
  }

  // Persist updated ELOs
  for (const [uid, elo] of Object.entries(playerElos)) {
    const isWinner = uid === winnerId;
    await supabase
      .from("users")
      .update({
        elo_rating: elo,
        total_wins: isWinner ? supabase.rpc("increment", { row_id: uid, col: "total_wins" }) : undefined,
        total_losses: !isWinner ? supabase.rpc("increment", { row_id: uid, col: "total_losses" }) : undefined,
      })
      .eq("id", uid);
  }
}
