# Supabase Architecture — Skrew (Digital Card Game)

This document provides a detailed overview of the Supabase backend architecture for the Skrew card game.

---

## 1. Database Schema (PostgreSQL)

The database is structured to handle real-time multiplayer state, user progression, and gamification.

### Core Tables
- **`users`**: Profile data, including `elo_rating`, `coins`, and stats. Linked to `auth.users`.
- **`matches`**: Authoritative match state (mode, status, current turn, discard pile).
- **`match_players`**: Player-specific match data (hand, seat index, team). *Note: The `hand` column is server-authoritative and restricted via RLS.*
- **`game_state`**: Hidden server-side state (the full `draw_pile` and `basra_queue`).

### Gamification & Social
- **`missions` & `user_missions`**: Daily/Weekly tasks and player progress.
- **`badges` & `user_badges`**: Achievement system.
- **`cosmetics` & `user_cosmetics`**: Monetization via card backs and table skins.
- **`messages`**: Real-time lobby and match chat.

---

## 2. Security & Access Control (RLS)

Skrew follows a **Server-Authoritative** model to prevent cheating.

- **Hidden Hands**: The `hand` column in `match_players` is only visible to the owner and the server (Edge Functions). Other players only see `hand_size`.
- **Game State Protection**: The `game_state` table (containing the draw pile) is completely inaccessible to clients (`USING (FALSE)`).
- **Match Membership**: Most RLS policies restrict data access to players who are members of the specific `match_id`.

---

## 3. Server-Side Logic (Edge Functions)

All game rules are enforced via Deno Edge Functions to ensure fairness and prevent client-side manipulation.

### Deployed Functions:
- **`deck-shuffle`**: Handles match initialization. It builds the deck based on the `game_mode`, shuffles using CSPRNG, deals hands, and transitions the match to the `info_phase`.
- **`basra-resolve`**: Manages the out-of-turn "Basra" race. It uses a server-side FIFO queue and timestamps to resolve concurrent card slaps fairly.
- **`score-calc`**: Triggered at the end of the match. Calculates final points, applies the "Screw" penalty (x2), and handles ELO rating updates.
- **`thief-guess`**: Resolves the "Thief" guessing phase. If the guess is wrong, scores between the caller and the thief holder are swapped.
- **`game-tick`**: A heartbeat function invoked by `pg_cron` every 5 seconds to expire turn timers for inactive players.
- **`admob-ssv`**: Handles Google AdMob Server-Side Verification. It verifies ECDSA signatures from Google before awarding coins to users.

---

## 4. Real-time Communication

- **Broadcast**: Used for high-frequency, low-latency events like "card swapped", "turn changed", and "basra success".
- **Presence**: Tracks player connectivity. If a player disconnects, the system can automatically transition them to a "bot" state or wait for a reconnect.
- **CDC (Change Data Capture)**: Used for the chat system (`messages` table) to ensure all players see new messages instantly.

---

## 5. Automation (pg_cron)

- **Turn Expiry**: `pg_cron` calls the `game-tick` Edge Function every minute (with internal 5s checks) to ensure matches don't stall.
- **Mission Resets**: Automated jobs reset daily and weekly mission progress.

---

## 6. Development Workflow

- **Migrations**: Managed via the Supabase CLI. Local changes are pulled into `supabase/migrations/` and pushed to production.
- **Edge Functions**: Developed in TypeScript (Deno). Shared logic (card definitions, admin client) is maintained in `supabase/functions/_shared/`.
