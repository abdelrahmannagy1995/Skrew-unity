// supabase/functions/deck-shuffle/index.ts
// Edge Function: Initialize a match, shuffle the deck, deal cards, open discard pile.

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import {
  buildFullDeck,
  filterDeckByMode,
  shuffleDeck,
  Card,
} from "../_shared/card-definitions.ts";
import {
  getAdminClient,
  badRequest,
  ok,
  internalError,
} from "../_shared/supabase-admin.ts";

interface DeckShuffleRequest {
  match_id: string;
  game_mode: "general" | "classic" | "thief" | "doubles";
  player_ids: string[]; // ordered by seat_index
}

const CARDS_PER_PLAYER = 4;
const INFO_PHASE_DURATION_SECS = 8; // duration for peek phase

serve(async (req: Request) => {
  if (req.method !== "POST") {
    return badRequest("Only POST is supported");
  }

  let body: DeckShuffleRequest;
  try {
    body = await req.json();
  } catch {
    return badRequest("Invalid JSON body");
  }

  const { match_id, game_mode, player_ids } = body;

  if (!match_id || !game_mode || !player_ids?.length) {
    return badRequest("match_id, game_mode, and player_ids are required");
  }

  if (player_ids.length < 3 || player_ids.length > 6) {
    return badRequest("Screw requires 3-6 players");
  }

  const supabase = getAdminClient();

  try {
    // Build and shuffle deck
    const fullDeck = buildFullDeck();
    const filteredDeck = filterDeckByMode(fullDeck, game_mode);
    const shuffled = shuffleDeck(filteredDeck);

    // Deal 4 cards to each player
    let deckIndex = 0;
    const playerHands: Record<string, Card[]> = {};

    for (const playerId of player_ids) {
      playerHands[playerId] = shuffled.slice(deckIndex, deckIndex + CARDS_PER_PLAYER);
      deckIndex += CARDS_PER_PLAYER;
    }

    // Open the discard pile with one card
    const discardTopCard = shuffled[deckIndex];
    deckIndex += 1;

    // Remaining cards = draw pile
    const drawPile = shuffled.slice(deckIndex);

    // Persist game_state
    const { error: gsError } = await supabase.from("game_state").insert({
      match_id,
      deck: shuffled,
      draw_pile: drawPile,
      basra_queue: [],
    });

    if (gsError) return internalError(`game_state insert failed: ${gsError.message}`);

    // Update each player's hand in match_players
    for (let i = 0; i < player_ids.length; i++) {
      const playerId = player_ids[i];
      const { error: mpError } = await supabase
        .from("match_players")
        .update({ hand: playerHands[playerId], hand_size: CARDS_PER_PLAYER })
        .eq("match_id", match_id)
        .eq("user_id", playerId);

      if (mpError) {
        return internalError(`match_players update failed for ${playerId}: ${mpError.message}`);
      }
    }

    // Update match: set discard_top_card, draw_pile_count, status = info_phase
    const { error: mError } = await supabase
      .from("matches")
      .update({
        status: "info_phase",
        discard_top_card: discardTopCard,
        discard_pile: [discardTopCard],
        draw_pile_count: drawPile.length,
      })
      .eq("id", match_id);

    if (mError) return internalError(`match update failed: ${mError.message}`);

    // Broadcast info-phase-start event to all clients
    await supabase.channel(`match:${match_id}`).send({
      type: "broadcast",
      event: "info_phase_start",
      payload: {
        duration_secs: INFO_PHASE_DURATION_SECS,
        // Each client receives only its own hand via a subsequent secure RPC
      },
    });

    return ok({
      success: true,
      draw_pile_count: drawPile.length,
      discard_top: discardTopCard,
      info_phase_duration_secs: INFO_PHASE_DURATION_SECS,
    });
  } catch (err) {
    console.error("[deck-shuffle] Unexpected error:", err);
    return internalError("Unexpected server error");
  }
});
