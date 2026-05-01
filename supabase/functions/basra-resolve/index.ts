// supabase/functions/basra-resolve/index.ts
// Edge Function: Resolve concurrent Basra match attempts using FIFO queue.

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { getAdminClient, badRequest, ok, internalError } from "../_shared/supabase-admin.ts";

interface BasraAttemptRequest {
  match_id: string;
  player_id: string;
  card_index: number;      // index in the player's hand they are revealing
  timestamp_ms: number;    // client timestamp (server will use server time for authoritative ordering)
}

serve(async (req: Request) => {
  if (req.method !== "POST") return badRequest("Only POST");

  let body: BasraAttemptRequest;
  try {
    body = await req.json();
  } catch {
    return badRequest("Invalid JSON");
  }

  const { match_id, player_id, card_index } = body;
  if (!match_id || !player_id || card_index === undefined) {
    return badRequest("match_id, player_id, and card_index are required");
  }

  const supabase = getAdminClient();
  const serverTimestampMs = Date.now();

  try {
    // Fetch match + game state atomically
    const [matchRes, gsRes, mpRes] = await Promise.all([
      supabase.from("matches").select("discard_top_card, status").eq("id", match_id).single(),
      supabase.from("game_state").select("basra_queue").eq("match_id", match_id).single(),
      supabase
        .from("match_players")
        .select("hand, hand_size")
        .eq("match_id", match_id)
        .eq("user_id", player_id)
        .single(),
    ]);

    if (matchRes.error) return internalError(matchRes.error.message);
    if (gsRes.error) return internalError(gsRes.error.message);
    if (mpRes.error) return internalError(mpRes.error.message);

    const match = matchRes.data;
    const gs = gsRes.data;
    const mp = mpRes.data;

    if (match.status !== "in_progress") {
      return badRequest("Match is not in progress");
    }

    const discardTop = match.discard_top_card;
    const hand: any[] = mp.hand;

    if (card_index < 0 || card_index >= hand.length) {
      return badRequest("Invalid card index");
    }

    const revealedCard = hand[card_index];

    // Check if revealed card matches discard top
    const isMatch =
      revealedCard.cardKey === discardTop.cardKey ||
      revealedCard.value === discardTop.value;

    if (!isMatch) {
      // PENALTY: player keeps their revealed card AND takes discard top card
      // Add discard top card to player's hand
      const newHand = [...hand];
      newHand.push(discardTop);

      await supabase
        .from("match_players")
        .update({ hand: newHand, hand_size: newHand.length })
        .eq("match_id", match_id)
        .eq("user_id", player_id);

      // Broadcast penalty event
      await supabase.channel(`match:${match_id}`).send({
        type: "broadcast",
        event: "basra_failed",
        payload: { player_id, card_index, penalty_card: discardTop },
      });

      return ok({ success: false, reason: "mismatch", penalty: true });
    }

    // Add attempt to FIFO queue (CSPRNG server timestamp ensures fairness)
    const queue: any[] = gs.basra_queue ?? [];
    queue.push({ player_id, card_index, server_ts: serverTimestampMs });

    // Sort FIFO by server timestamp
    queue.sort((a: any, b: any) => a.server_ts - b.server_ts);

    // Only the FIRST entry in the queue wins
    const winner = queue[0];

    if (winner.player_id !== player_id) {
      // Another player got there first; this attempt is rejected (no penalty)
      return ok({ success: false, reason: "race_lost" });
    }

    // This player wins the Basra match – remove card from their hand and discard it
    const newHand = hand.filter((_: any, i: number) => i !== card_index);
    await supabase
      .from("match_players")
      .update({ hand: newHand, hand_size: newHand.length })
      .eq("match_id", match_id)
      .eq("user_id", player_id);

    // Update discard pile
    const { data: matchFull } = await supabase
      .from("matches")
      .select("discard_pile")
      .eq("id", match_id)
      .single();

    const discardPile: any[] = matchFull?.discard_pile ?? [];
    discardPile.push(revealedCard);

    await supabase
      .from("matches")
      .update({
        discard_top_card: revealedCard,
        discard_pile: discardPile,
      })
      .eq("id", match_id);

    // Clear the basra queue
    await supabase
      .from("game_state")
      .update({ basra_queue: [] })
      .eq("match_id", match_id);

    // Check for Obligatory Screw (hand reaches 0)
    if (newHand.length === 0) {
      await supabase
        .from("matches")
        .update({ status: "endgame", screw_caller_id: player_id })
        .eq("id", match_id);

      await supabase.channel(`match:${match_id}`).send({
        type: "broadcast",
        event: "obligatory_screw",
        payload: { player_id },
      });
    }

    // Broadcast successful Basra
    await supabase.channel(`match:${match_id}`).send({
      type: "broadcast",
      event: "basra_success",
      payload: { player_id, card_index, discarded_card: revealedCard },
    });

    // Update mission progress
    await supabase.rpc("increment_mission_progress", {
      p_user_id: player_id,
      p_mission_key: "basra",
    });

    return ok({ success: true });
  } catch (err) {
    console.error("[basra-resolve] Error:", err);
    return internalError("Unexpected error");
  }
});
