# Codebase Reference — Skrew

> **Audience:** Engineers onboarding to Skrew, or auditors looking up specific symbols.
> **Last reviewed:** 2026-04-28
> **Convention:** Top-down per namespace; fully-qualified names; line-numberless (file references only — open the file in your editor).

This document is **descriptive** (it tells you *what's there*); the **prescriptive** docs are `ARCHITECTURE.md`, `DESIGN_PATTERNS.md`, and `SOP.md`.

---

## Top-Level Layout

```
/
├── Assets/                     # Unity assets (everything imported into the engine)
│   ├── csc.rsp                 # C# compiler flags (warnings as errors)
│   ├── Resources/
│   │   └── Localization/       # Loaded at runtime via Resources.Load<TextAsset>
│   │       ├── en.json
│   │       └── ar.json
│   └── Scripts/
│       ├── AI/                 # MCTS agent + Unity wrapper
│       ├── Core/               # Singletons, deck, game loop
│       ├── Entities/           # Card MonoBehaviours
│       ├── Gamification/       # Streaks, missions, AdMob
│       ├── Localization/       # i18n + RTL
│       ├── Networking/         # Realtime Presence + ephemeral broadcast
│       ├── Social/             # Persistent chat
│       ├── StateMachines/      # Phase states
│       └── UI/                 # HUD, grids, modals, effects
├── Packages/manifest.json      # UPM packages + scoped registries (OpenUPM)
├── ProjectSettings/
│   └── ProjectVersion.txt      # Pinned to Unity 6000.3.14f1 LTS
├── docs/                       # This documentation suite
└── supabase/                   # Backend (separate from Unity)
    ├── config.toml
    ├── functions/              # Deno Edge Functions (kebab-case folders)
    └── migrations/             # Versioned SQL
```

---

## Engine & Tooling

| Item | Pinned Version | Source |
|------|---------------|--------|
| Unity Editor | **6000.3.14f1 LTS** | `ProjectSettings/ProjectVersion.txt` |
| Cinemachine | 3.1.2 | `Packages/manifest.json` |
| Input System | 1.11.2 | `Packages/manifest.json` |
| TextMeshPro | 3.2.0-pre.10 | `Packages/manifest.json` |
| Newtonsoft.Json | 3.2.1 | `Packages/manifest.json` |
| UniTask | via OpenUPM (`com.cysharp.unitask`) | `Packages/manifest.json` |
| NuGetForUnity | via OpenUPM (`com.github-glitchenzo.nugetforunity`) | `Packages/manifest.json` |
| C# language version | 9.0 (warnings as errors) | `Assets/csc.rsp` |

> **Why Unity 6 LTS?** Two-year support window, native support for the latest iOS/Android SDKs, improved WebGL build pipeline (Brotli pre-compressed delivery), and stable URP for our 2D art.

---

## Namespace: `ScrewGame.Core`

### `SupabaseManager : MonoBehaviour` *(singleton)*
**File:** `Assets/Scripts/Core/SupabaseManager.cs`
**Responsibility:** Owns the `Supabase.Client`. Bootstraps anon-key auth, exposes the `CurrentUserId`, and wires `UniTaskScheduler` so every `await` resumes on Unity's main thread.

| Member | Description |
|--------|-------------|
| `static Instance { get; }` | Cross-scene singleton |
| `Client` | The `Supabase.Client` instance |
| `CurrentUserId` | Cached user UUID (or null pre-login) |
| `InitialiseAsync(CancellationToken)` | Boots auth, refreshes session |
| `SignInAnonymouslyAsync()` | First-time onboarding flow |

### `DeckManager` *(static)*
**File:** `Assets/Scripts/Core/DeckManager.cs`
**Responsibility:** Builds a 66-card client-side deck array used **only for AI determinization and offline simulation**. The authoritative deck is shuffled server-side.

| Member | Description |
|--------|-------------|
| `BuildDeck(GameMode mode)` | Returns the complete unshuffled deck filtered by mode |
| `enum GameMode { General, Classic, Thief, Doubles }` | Four canonical variants |

### `GameLoop : MonoBehaviour` *(partial, singleton)*
**Files:** `Assets/Scripts/Core/GameLoop.cs` + `Assets/Scripts/Core/GameLoopActions.cs`
**Responsibility:** Single client-side façade for both inbound (Realtime Broadcast) and outbound (Edge Function) traffic. Pushes broadcast events to the active `GameStateMachine`; provides typed Edge-Function caller methods.

**Inbound API**
| Member | Description |
|--------|-------------|
| `JoinMatchAsync(matchId, ct)` | Subscribe to `match:<id>` broadcast channel |
| `static GameEvents.OnPresenceUpdated` | Static delegate fired on presence sync |

**Outbound API (player actions)** — all are `UniTask<bool>` returning success:
| Method | Edge Function |
|--------|---------------|
| `DrawFromStockAsync` | `draw-from-stock` |
| `SwapDrawnCardAsync(int gridIndex)` | `swap-drawn-card` |
| `DiscardDrawnCardAsync` | `discard-drawn-card` |
| `SnatchDiscardAsync` | `snatch-discard` |
| `DeclareScrewAsync` | `declare-screw` |
| `AttemptBasraAsync(int cardIndex)` | `basra-resolve` (with `timestamp_ms`) |
| `PeekOpponentCardAsync(opponentId, cardIndex)` | `peek-opponent` |
| `DiscardOwnCardAsync(int cardIndex)` | `basra-self` |
| `BlindSwapAsync(ownIndex, opponentId, oppIndex)` | `blind-swap` |
| `GiveCardToOpponentAsync(ownIndex, opponentId)` | `give-card` |
| `KaabDayerAllPlayersAsync` | `kaab-dayer` |
| `AgabMaAgabAsync(opponentId, cardIndex)` | `agab-ma-agab` |
| `ForceSwapThiefAsync(int slotIndex)` | `force-swap-thief` |
| `PlayPingPongAsync(SpecialCardId)` | `play-ping-pong` |
| `SubmitThiefGuessAsync(string guessedPlayerId)` | `thief-guess` |

---

## Namespace: `ScrewGame.Entities`

### `enum CardType { Numerical, Command, Special }`

### `enum CommandCardId`
- `PeekSelf` (7/8) · `PeekOpponent` (9/10) · `Basra` · `KhodWHat` · `KhodBas` · `KaabDayer` · `AgabMaAgab` · `AlaKefak`

### `enum SpecialCardId { None, Thief, Ping, Pong, GreenScrew, RedScrew }`

### `[Serializable] CardData`
Plain data class serialised over the wire by Newtonsoft.Json. Holds `CardKey`, `CardType`, `Value`, `CommandId`, `SpecialId`, plus `[NonSerialized]` UI flags `IsRevealed` and `InLocalGrid`.

### `CardObject : MonoBehaviour`
Base prefab driver. Methods: `Initialise(data, gridIndex)`, `SetFaceUp/Down`, `ToggleFace`. Hook: `virtual OnCommandActivated()` (default no-op).

### `CommandCardObject : CardObject`
Overrides `OnCommandActivated()` to dispatch on `Data.CommandId`. Each branch invokes `CommandUI.Instance` to gather user input, then calls the appropriate `GameLoop` Edge Function method. The `AlaKefak` (wildcard) branch swaps `Data` in place — *never* creates and destroys a temporary component (avoids killing in-flight coroutines).

### `SpecialCardObject : CardObject`
Handles Thief (forced grid swap), Ping/Pong (broadcast skip), Red Screw burn FX. Green Screw is rendered but has no active effect when discarded (worth 0 points only).

---

## Namespace: `ScrewGame.StateMachines`

### `abstract class GameState`
- `protected Dictionary<string, object> Payload` — the payload that triggered the transition.
- Hooks: `OnEnter(machine)`, `OnUpdate(machine)`, `OnExit(machine)`.
- `protected T GetPayloadValue<T>(string key, T defaultValue = default)` — typed accessor.

### `class GameStateMachine : MonoBehaviour`
- `TransitionTo(GameState newState)` — calls `OnExit` on outgoing, `OnEnter` on incoming.
- Routes broadcast events to handler interfaces:
  - `IBasraHandler.HandleBasraSuccess / HandleBasraFailed`
  - `IThiefHandler.HandleThiefGuessResult`
  - `IPingPongHandler.HandlePingPongSkip`

### Concrete states (in `GamePhaseStates.cs`)
| State | Triggered by | Notable behaviour |
|-------|--------------|-------------------|
| `InfoPhaseState` | `match_started` broadcast | Reveals indices 2 & 3 for `duration_secs` (default 8); transitions to `PlayerTurnState` |
| `PlayerTurnState` | `turn_changed` broadcast | Unlocks `TurnControlPanel` if `currentSeat == localSeat`; implements `IBasraHandler`, `IPingPongHandler` |
| `ResolveActionState` | Command card played | Disables turn controls until the effect is broadcast as resolved |
| `MatchEndState` | `screw_called` / `obligatory_screw` | Reveals all grids; spawns `ThiefGuessModal` to caller if Thief mode active; implements `IThiefHandler` |

---

## Namespace: `ScrewGame.AI`

### `class AiMemoryMatrix`
**File:** `Assets/Scripts/AI/ScrewAiAgent.cs`
- `Learn(playerId, index, card)` — record a revealed card.
- `ForgetAfterBlindSwap(playerId, index)` — full forget on blind swap.
- `ApplyDecay()` — once per turn; each known card forgotten with prob `1 - retentionRate`.
- `EstimateScore(playerId, unknownCardMean)` — sum of known + mean for unknown.

### `class ScrewAiAgent`
- Constructor: `(aiPlayerId, allPlayerIds, gridSize, initialDifficulty)`.
- `ChooseAction(currentState, legalActions)` — random under difficulty `< 0.2`, MCTS otherwise.
- `AdjustDifficulty(humanScoreDelta)` — proportional DDA, clamped `±0.15`.
- Internal MCTS phases: `Select`, `Expand`, `Simulate`, `Backpropagate` operating over `MctsNode` tree.
- `OnCardRevealed`, `OnBlindSwapOccurred`, `OnTurnAdvanced` — memory hooks called from `AiPlayerController`.

### `class AiPlayerController : MonoBehaviour`
**File:** `Assets/Scripts/AI/AiPlayerController.cs`
- Inspector: `_initialDifficulty`, `_botPlayerId`, `_thinkDelayMs`.
- `Initialise(allPlayerIds, gridSize)` — spawn the agent.
- `TakeTurnAsync(gameState, ct)` — wait `_thinkDelayMs`, decide, execute via `GameLoop`, then `OnTurnAdvanced`.

---

## Namespace: `ScrewGame.Networking`

### `class PresenceManager : MonoBehaviour` *(singleton)*
- `JoinAsync(matchId, ct)` — track presence on `presence:match:<id>` channel.
- Events: `OnPlayerConnected`, `OnPlayerDisconnected` (latter triggers bot fallback in higher-level `GameManager`).
- *Implementation note:* uses a single `IEnumerator` per handler — do **not** call `.GetEnumerator()` twice on the same dictionary.

### `class EmojiStickerBroadcast : MonoBehaviour` *(singleton)*
- `JoinAsync(matchId, ct)` — subscribe to `social:match:<id>`.
- `SendEmojiAsync(emojiCode, ct)` / `SendStickerAsync(stickerId, ct)` — broadcast only, **no DB write**.
- Events: `OnEmojiReceived`, `OnStickerReceived`.

---

## Namespace: `ScrewGame.Social`

### `class ChatMessage : Postgrest.Models.BaseModel`
Maps to public `messages` table. Properties: `Id`, `MatchId`, `UserId`, `Content`, `CreatedAt`.

### `class ChatManager : MonoBehaviour` *(singleton)*
- `JoinChatAsync(matchId, ct)` — subscribe to Postgres CDC on `messages` filtered to this match.
- `LoadHistoryAsync(limit, ct)` — paginated past 50 messages.
- `SendMessageAsync(content, ct)` — server-side validation: ≤ 500 chars; trims; rejects empty.
- Event: `OnNewMessage`.

---

## Namespace: `ScrewGame.Gamification`

### `class UserProfile / Mission / UserMission / LeaderboardEntry : BaseModel`
Plain Postgrest models matching the equivalent tables. **No setter validation** — DB constraints + RLS are the source of truth.

### `class GamificationManager : MonoBehaviour` *(singleton)*
- `UpdateStreakAsync(ct)` — call on login. Resets streak if last login > 1 day ago, increments otherwise. Awards escalating coin bonus (10→20→30→50→75→100+).
- `GetProfileAsync(ct)` — cached.
- `GetActiveMissionsAsync(period, ct)` — for daily/weekly UI.
- `GetLeaderboardAsync(limit, ct)` — Classic-mode ELO leaderboard view.

### `class AdMobManager : MonoBehaviour` *(singleton)*
- Inspector: `_rewardedAdUnitId`.
- Loads/shows rewarded ads via `GoogleMobileAds.Api`.
- Sets `ServerSideVerificationOptions.SetCustomData(JSON({user_id}))` so the **server** SSV Edge Function knows which player to credit.
- Events: `OnRewardGranted(int amount)`, `OnAdFailedToLoad`.

---

## Namespace: `ScrewGame.Localization`

### `class LocalizationManager : MonoBehaviour` *(singleton)*
- `SetLocale(locale)` — switches `en` / `ar` and reloads bundle from `Resources/Localization/`.
- `Get(key)` — returns localised string; if `IsRtl`, runs through `RtlProcessor.Process`.
- `static T(key)` — short alias.
- Event: `OnLanguageChanged` (fires after a successful switch).

### `static class RtlProcessor`
- `Process(string)` — calls `RTLTMPro.RTLSupport.FixRTL` directly when the symbol `RTLTMPRO_IMPORTED` is defined; otherwise reflection-loads the type so the project compiles even before the plug-in is imported. Last-resort fallback reverses the string.

---

## Namespace: `ScrewGame.UI`

| Class | Role |
|-------|------|
| `PlayerGrid : MonoBehaviour` | Local 4-card grid; DOTween animations (`AnimateCardDeal`, `AnimateSwap`); `LocalInstance` accessor. |
| `HUDController : MonoBehaviour` | Top-bar messages, countdown timer, seat highlight ring. |
| `TurnControlPanel : MonoBehaviour` | Wraps Draw / Snatch / Screw buttons; `SetInteractable(bool)`. |
| `VisualEffects : MonoBehaviour` | Cinemachine perlin-noise screen shake on Basra; `ParticleSystem` burst on success. |
| `ScoreboardUI : MonoBehaviour` (stub) | End-of-match modal showing `scores` and `winner_id`. |
| `ThiefGuessModal : MonoBehaviour` (stub) | Single-question modal: "Who holds the Thief?". |
| `AllGridsRevealController : MonoBehaviour` (stub) | Flips every card face-up at match end. |
| `OpponentGridManager : MonoBehaviour` (stub) | Mirror of `PlayerGrid` for remote players. |
| `CommandUI : MonoBehaviour` (stub) | Modal dialog system used by every `CommandCardObject` branch. |
| `static class PlayerSeatRegistry` | `Register(playerId, seatIndex)` / `GetLocalSeat(playerId)`. |

> **Stubs** are intentional — they expose the contracts so other systems compile and the visual layer can be filled in by the UI/UX team in Q2 2026 (see roadmap).

---

## Backend: `supabase/`

### Migrations
| File | Adds |
|------|------|
| `001_initial_schema.sql` | `users`, `matches`, `match_players`, `game_state` (no client reads), `messages`, `missions`, `user_missions`, `badges`, `user_badges`, `cosmetics`, ELO leaderboard view, `updated_at` triggers; sets `log_statement='mod'` at the **database** level (not cluster role). |
| `002_rls_policies.sql` | RLS for every public table. `game_state`: `USING (FALSE)` for all client policies. `match_players.hand`: column-level revoke from anon. `messages`: scoped by `match_id` membership. |
| `003_cron_jobs.sql` | `pg_cron` jobs: 5 s heartbeat (`game-tick`), midnight UTC daily mission reset, weekly mission reset on Mondays. |
| `004_seed_data.sql` | Five badges, four daily missions, three weekly missions, five cosmetic card backs. |
| `005_helper_rpcs.sql` | `increment_user_stat`, `award_coins` (idempotent via `coin_transactions.transaction_id UNIQUE`), `increment_mission_progress`. |

### Edge Functions
| Folder | Endpoint | Purpose |
|--------|----------|---------|
| `_shared/card-definitions.ts` | (library) | 66-card builder, mode filters, CSPRNG `crypto.getRandomValues` shuffle |
| `_shared/supabase-admin.ts` | (library) | Admin client + typed JSON response helpers |
| `deck-shuffle/index.ts` | `POST /deck-shuffle` | Shuffle, deal 4/player, open discard, broadcast `info_phase_start` |
| `game-tick/index.ts` | `POST /game-tick` | Invoked by `pg_cron`; expires turns; broadcasts `turn_changed` |
| `basra-resolve/index.ts` | `POST /basra-resolve` | FIFO concurrent resolution; penalty on miss; Obligatory Screw on empty hand |
| `score-calc/index.ts` | `POST /score-calc` | Final scoring with x2 caller penalty; per-player ELO via `increment_user_stat` RPC |
| `thief-guess/index.ts` | `POST /thief-guess` | Correct ⇒ neutralise Thief; wrong ⇒ swap caller / Thief-holder scores |
| `admob-ssv/index.ts` | `POST /admob-ssv` | Verifies Google's ECDSA P-256 signature; calls `award_coins` RPC |

### Configuration
- `supabase/config.toml` — TOML (not JSON). Project ID, ports, per-function `verify_jwt` flags. SSV and game-tick set `verify_jwt = false` because they receive third-party callbacks.

---

## Localization Strings

`Assets/Resources/Localization/en.json` and `ar.json` ship with **40+ keys** covering UI labels, card names, mode names, and broadcast messages. Adding a key requires adding it to **both** files (PR checklist enforced).

| Key prefix | Domain |
|------------|--------|
| `app_*` | Brand |
| `btn_*` | Buttons |
| `mode_*` | Game modes |
| `card_*` | Card display names |
| `msg_*` | Toast / banner messages |
| `lbl_*` | Form labels and headings |

---

## Build & Test Commands

| Command | Purpose |
|---------|---------|
| Open in Unity Hub | Editor opens project at the pinned `6000.3.14f1` LTS |
| `Window → General → Test Runner → EditMode → Run All` | All non-PlayMode tests |
| `Window → General → Test Runner → PlayMode → Run All` | Scene-bound tests |
| `supabase db push` | Apply migrations to linked project |
| `supabase functions deploy` | Deploy all Edge Functions |
| `supabase functions logs <name> --tail` | Live logs for an Edge Function |

CI pipelines (planned in `.github/workflows/` for the Q3 2026 milestone):
- `ci-unity-tests.yml` — EditMode + PlayMode test runner per PR.
- `ci-supabase-lint.yml` — Deno lint + SQL syntax check.
- `cd-edge-functions.yml` — Deploy on `main` push.
- `cd-mobile-builds.yml` — Unity Cloud Build for iOS/Android on tagged releases.

---

## Glossary

| Term | Meaning |
|------|---------|
| **Basra** | Out-of-turn rule allowing a player to discard a matching card immediately. |
| **Khod w Hat** | Blind swap command between own and opponent grid. |
| **Khod Bas** | "Give only" command — push a card to an opponent without taking one. |
| **Kaab Dayer** | "Spin" — peek one card from every player **or** two of your own. |
| **3agab ma 3agab** | Conditional peek-and-optionally-swap command. |
| **3ala Kefak** | Wildcard — emulates any other command. |
| **Screw / سكرو** | Declaration that you believe you have the lowest score. |
| **Obligatory Screw** | Auto-trigger when your hand reaches 0 cards. |
| **DDA** | Dynamic Difficulty Adjustment. |
| **MCTS** | Monte Carlo Tree Search. |
| **SSV** | Server-Side Verification (AdMob). |
| **CSPRNG** | Cryptographically Secure Pseudo-Random Number Generator. |
| **CDC** | Change Data Capture (Postgres → Realtime). |
| **RLS** | Row-Level Security (Postgres). |

---

## Where to Go Next

- **New feature?** Read `ARCHITECTURE.md` first, then `DESIGN_PATTERNS.md`.
- **On-call?** Read `SOP.md` (esp. § 3 Incident Response).
- **Roadmap question?** Read `FUTURE_PLAN.md`.
- **Bug fix?** Reproduce locally, write a failing test in the appropriate namespace's test folder, then fix.

If a section of this document drifts out of date, a PR labelled `docs/codebase` is the only correct fix.
