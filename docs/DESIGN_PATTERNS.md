# Design Patterns Catalogue — Skrew

> **Audience:** Engineers contributing to Skrew. Use this as a reference for *what* pattern is used, *where* it lives in the codebase, and *why* it was chosen.
> **Last reviewed:** 2026-04-28

The codebase deliberately uses well-known patterns from *Design Patterns* (Gamma et al.), *Game Programming Patterns* (Robert Nystrom), and modern C# idioms. This document indexes them with concrete file references.

---

## 1. Creational Patterns

### 1.1 Singleton (with Unity scene lifecycle)
**Where**
- `Assets/Scripts/Core/SupabaseManager.cs` — `Instance` static
- `Assets/Scripts/Core/GameLoop.cs`
- `Assets/Scripts/UI/UIControllers.cs` — `HUDController`, `TurnControlPanel`, `VisualEffects`, `ScoreboardUI`, `ThiefGuessModal`, `CommandUI`, `OpponentGridManager`
- `Assets/Scripts/Networking/PresenceManager.cs`
- `Assets/Scripts/Networking/EmojiStickerBroadcast.cs`
- `Assets/Scripts/Social/ChatManager.cs`
- `Assets/Scripts/Localization/LocalizationManager.cs`
- `Assets/Scripts/Gamification/GamificationManager.cs`, `AdMobManager.cs`

**Why**
- Each manager has at most one logical instance per game session.
- `MonoBehaviour`-bound, so they participate in Unity's scene lifecycle (`Awake`, `OnDestroy`).
- Use the safe `Instance != null && Instance != this → Destroy(gameObject)` idiom to survive scene reloads.

**Trade-off** — singletons are global state; mitigated by:
- Limiting them to *managers* (no game-logic singletons).
- Documenting them all here so devs know the inventory.

### 1.2 Factory Method (Deck Building)
**Where** `Assets/Scripts/Core/DeckManager.cs::BuildDeck(GameMode)` and `supabase/functions/_shared/card-definitions.ts::buildBaseDeck()`
**Why** Centralises the rules for generating a 66-card deck variant (General, Classic, Thief, Doubles). Callers never construct cards directly.

### 1.3 Object Pool *(planned)*
**Where** Card instantiation in `PlayerGrid` will move to a pool to avoid GC churn during animation-heavy phases.
**Status** Tracked in `FUTURE_PLAN.md` — Q3 2026.

---

## 2. Structural Patterns

### 2.1 Adapter
**Where** `Assets/Scripts/Localization/LocalizationManager.cs::RtlProcessor`
**Why** Wraps the third-party `RTLTMPro.RTLSupport.FixRTL()` API behind a stable internal contract. Uses reflection so the project compiles even before the plug-in is imported.

### 2.2 Façade
**Where** `Assets/Scripts/Core/GameLoop.cs` (+ partial `GameLoopActions.cs`)
**Why** Single object presents a coherent player-action API (`DrawFromStockAsync`, `DeclareScrewAsync`, `BlindSwapAsync`, …) hiding the underlying `Supabase.Functions.Invoke` plumbing.

### 2.3 Decorator *(implicit)*
**Where** `RtlProcessor.Process` decorates raw localised strings with letter-shaping & direction marks before they reach TMP.

### 2.4 Composite
**Where** `Assets/Scripts/UI/UIControllers.cs::PlayerGrid` composes 4 `CardObject` children. The `MatchEndState` reveals all grids via `AllGridsRevealController` operating on the composite tree.

---

## 3. Behavioural Patterns

### 3.1 State (★ central pattern)
**Where**
- Base: `Assets/Scripts/StateMachines/GameState.cs`
- Concrete: `Assets/Scripts/StateMachines/GamePhaseStates.cs` — `InfoPhaseState`, `PlayerTurnState`, `ResolveActionState`, `MatchEndState`
- Context: `GameStateMachine` (MonoBehaviour)

**Why**
- The match has clearly distinct *phases* (peek, turn, resolve action, end) with phase-specific input handling.
- `GameStateMachine.TransitionTo()` standardises `OnEnter` / `OnExit` lifecycles, eliminating ad-hoc booleans.
- Cross-cutting concerns (Basra, Thief, Ping/Pong) are routed via marker interfaces (`IBasraHandler`, `IThiefHandler`, `IPingPongHandler`) so only states that care receive them.

**Diagram** — see `docs/ARCHITECTURE.md` § 5.

### 3.2 Observer
**Where**
- `static class GameEvents` in `GameLoop.cs` — `OnPresenceUpdated`, `OnBasraOpportunity`, `OnTurnChanged`
- `PresenceManager.OnPlayerConnected` / `OnPlayerDisconnected`
- `EmojiStickerBroadcast.OnEmojiReceived` / `OnStickerReceived`
- `ChatManager.OnNewMessage`
- `GamificationManager.OnLanguageChanged` (`LocalizationManager`)

**Why** Decouples the source of an event (a Supabase Realtime payload) from the many listeners (UI overlays, particle systems, AI memory updates).

### 3.3 Command *(implicit, via Edge Function calls)*
**Where** Each `GameLoopActions.cs` method (`DrawFromStockAsync`, `BlindSwapAsync`, `AttemptBasraAsync`, …) packages a player intent into a serialisable JSON object posted to a uniquely named Edge Function.
**Why** Server can log, replay, and audit every command from `match_actions` (planned for the replay system in Q4 2026 — see roadmap).

### 3.4 Strategy
**Where**
- `Assets/Scripts/AI/ScrewAiAgent.cs::ChooseAction` — branches by difficulty: random for difficulty < 0.2, MCTS otherwise.
- DDA effectively swaps the strategy at runtime by adjusting `_mctsIterations` and `_retentionRate`.

### 3.5 Template Method
**Where** `Assets/Scripts/Entities/CardObject.cs::OnCommandActivated()` is the hook overridden by `CommandCardObject` and `SpecialCardObject`. The base class fixes the lifecycle (`Initialise → SetFaceDown / SetFaceUp → OnCommandActivated`).

### 3.6 Chain of Responsibility *(implicit)*
**Where** Realtime broadcast events arrive at `GameLoop`, which hands off to `GameStateMachine`, which checks the active state for the matching handler interface. Unhandled events fall through silently.

### 3.7 Mediator
**Where** `GameLoop.Instance` mediates between UI controls, AI, and Supabase. UI components never call Supabase directly.

### 3.8 Memento *(planned)*
**Where** `match_actions` table (Q4 2026 roadmap) will snapshot every action so any match can be replayed deterministically.

### 3.9 Iterator
**Where** Standard `foreach` use throughout. **Anti-pattern fixed** in `PresenceManager.HandlePresenceJoin/Leave`: previous code called `.GetEnumerator()` twice on the same collection (creating two enumerators); the corrected code stores the enumerator in a variable.

---

## 4. Game-Programming-Specific Patterns

### 4.1 Game Loop
**Where**
- Server: `supabase/functions/game-tick/index.ts` invoked by `pg_cron` every 5 s. Detects expired turns and advances the seat pointer. The Edge Function is the *authoritative* loop tick.
- Client: Unity's standard `Update()` drives the state machine's `OnUpdate(machine)` per frame.

### 4.2 Update Method
**Where** `GameStateMachine.Update()` delegates to the active state's `OnUpdate(this)` so per-state polling logic stays encapsulated (e.g., `InfoPhaseState` countdown).

### 4.3 Type Object
**Where** `Assets/Scripts/Entities/CardObject.cs::CardData` is a *type object* — the same `MonoBehaviour` (`CardObject`) is parameterised by a `CardData` instance describing its kind. New card types are added by creating a new `CardData` (or extending `CommandCardId` / `SpecialCardId`) without touching the renderer.

### 4.4 Spatial Partition *(N/A)*
Not used — the game is 2D card-based with a small object count.

### 4.5 Service Locator
**Where** Implicit via the singleton `Instance` accessor on every manager. Considered for upgrade to `VContainer` DI in Q1 2027.

### 4.6 Component
**Where** Pervasive — every Unity object is a composition of `MonoBehaviour` components (e.g., `CardObject` + `SpriteRenderer` + `BoxCollider2D` + `Animator`).

### 4.7 Bytecode *(N/A)*
Not used — game logic is direct C# / TypeScript.

### 4.8 Subclass Sandbox
**Where** `CardObject` provides primitives (`SetFaceUp`, `SetFaceDown`) that subclasses (`CommandCardObject`, `SpecialCardObject`) compose to implement effects.

### 4.9 Double Buffer *(N/A)*
Unity's render pipeline already handles this internally.

### 4.10 Event Queue
**Where** `game_state.basra_queue` (Postgres JSONB / table) — concurrent Basra attempts are queued and resolved FIFO using `FOR UPDATE SKIP LOCKED`. See `supabase/functions/basra-resolve/index.ts`.

---

## 5. Concurrency / Async Patterns

### 5.1 Async/Await with Cancellation Tokens
**Where** Every public async method in `GameLoop`, `GamificationManager`, `ChatManager`, `EmojiStickerBroadcast`, and `PresenceManager` accepts a `CancellationToken`. Required by `UniTask` to integrate with Unity's main-thread scheduler.

### 5.2 Producer–Consumer
**Where** Realtime Broadcast = producers (Edge Functions, peers); `GameLoop` event handlers = consumers.

### 5.3 First-In-First-Out Resolution
**Where** Basra concurrency (see `basra-resolve` Edge Function). Server timestamp is the canonical ordering key.

### 5.4 Idempotent Receiver
**Where** `coin_transactions.transaction_id UNIQUE` constraint plus the `award_coins(p_user_id, p_coins, p_transaction_id, p_source)` RPC. Prevents double-grants when AdMob retries the SSV callback.

---

## 6. Domain-Specific Patterns

### 6.1 Authoritative Server
**Where** All state mutations route through `supabase/functions/*` (Deno). Clients never write to `game_state` directly. Postgres RLS enforces this with `CREATE POLICY game_state_no_client ON game_state FOR ALL USING (FALSE);`.

### 6.2 Imperfect-Information Game-Tree Search
**Where** `Assets/Scripts/AI/ScrewAiAgent.cs` — MCTS with **determinization**: unknown opponent cards are sampled from the remaining-deck distribution before each rollout.

### 6.3 Intentional Forgetting
**Where** `Assets/Scripts/AI/ScrewAiAgent.cs::AiMemoryMatrix.ApplyDecay` — probabilistic memory decay; full memory clear on `ForgetAfterBlindSwap`. Mimics human cognitive load.

### 6.4 Dynamic Difficulty Adjustment (DDA)
**Where** `ScrewAiAgent.AdjustDifficulty` — proportional adjustment of `_difficultyLevel` based on `humanScoreDelta`, clamped to `±0.15` per round.

### 6.5 Server-Side Verification (SSV)
**Where** `supabase/functions/admob-ssv/index.ts` — verifies Google's ECDSA P-256 signature using the public key fetched by `key_id` before crediting coins.

### 6.6 Row-Level Security as Authorisation Layer
**Where** `supabase/migrations/002_rls_policies.sql`. Pattern: the database itself is the last line of defence — even with a leaked anon key, an attacker cannot read another player's hidden cards.

### 6.7 CSPRNG for Game-Critical Randomness
**Where** `supabase/functions/_shared/card-definitions.ts::shuffleDeck` uses `crypto.getRandomValues` (Deno's Web Crypto API), not `Math.random()`. Required for fairness audits.

---

## 7. Anti-Patterns Avoided

| Anti-pattern | Why we avoid it | Where mitigated |
|--------------|----------------|------------------|
| Client-side authority | Trivially cheatable | Edge Functions + RLS |
| God objects | Hard to test, scales poorly | `GameLoop` is split via `partial` and delegates to managers |
| Stringly-typed enums | Refactor breakage | `CardType`, `CommandCardId`, `SpecialCardId`, `GameMode` enums |
| Async-void | Unhandled exceptions crash Unity | `UniTaskVoid` only at top-level entry points |
| Magic numbers | Unreviewable constants | Named constants (`BaseMctsIterations`, `UnknownCardMean`) |
| Reflection at runtime hot paths | Allocation + JIT cost | `RtlProcessor` reflection is one-shot at locale load |

---

## 8. Pattern Adoption Heuristics

When proposing a new pattern in a PR:

1. **Document it here first.** A pattern that isn't catalogued is invisible.
2. **Justify uniqueness.** "We already have State / Observer — does the new pattern actually solve something different?"
3. **Demonstrate testability.** Can the pattern be exercised by an EditMode test?
4. **Estimate cost.** Allocation, abstraction overhead, cognitive load.

> *"A pattern is a solution to a problem in a context — name the problem, then the pattern."* — Christopher Alexander.
