# Skrew (سكرو) – Digital Card Game

A full-stack digital adaptation of the Egyptian card game **Screw (سكرو)** built with:

- **Unity 2022+** (C#) – iOS, Android, WebGL
- **Supabase** – PostgreSQL, Realtime, Auth, Edge Functions, Storage
- **UniTask** – Non-blocking async Unity coroutines
- **DOTween** – Smooth card animations
- **Cinemachine** – Screen shake on Basra impacts
- **RTLTMPro** – Right-to-left Arabic text rendering
- **Google AdMob** – Rewarded video ads with server-side verification

---

## Repository Structure

```
Skrew-unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/               # Game loop, Supabase client, DeckManager
│   │   ├── StateMachines/      # GameState base + phase states
│   │   ├── Entities/           # CardObject, CommandCardObject, SpecialCardObject
│   │   ├── AI/                 # MCTS agent + AiPlayerController (DDA)
│   │   ├── UI/                 # HUD, grids, visual effects, modals
│   │   ├── Networking/         # PresenceManager, EmojiStickerBroadcast
│   │   ├── Social/             # ChatManager
│   │   ├── Gamification/       # GamificationManager, AdMobManager
│   │   └── Localization/       # LocalizationManager, RtlProcessor
│   ├── Prefabs/                # Card Token, UI dialogs, Localized text containers
│   └── Resources/
│       └── Localization/
│           ├── en.json         # English strings
│           └── ar.json         # Egyptian Arabic strings
└── supabase/
    ├── config.toml
    ├── migrations/
    │   ├── 001_initial_schema.sql
    │   ├── 002_rls_policies.sql
    │   ├── 003_cron_jobs.sql
    │   └── 004_seed_data.sql
    └── functions/
        ├── _shared/
        │   ├── card-definitions.ts   # Deck building, filtering, CSPRNG shuffle
        │   └── supabase-admin.ts     # Admin client + response helpers
        ├── deck-shuffle/index.ts     # Match init: shuffle, deal, open discard
        ├── game-tick/index.ts        # pg_cron heartbeat: expire turn timers
        ├── basra-resolve/index.ts    # Concurrent Basra FIFO resolution
        ├── score-calc/index.ts       # Final scoring, x2 Screw penalty, ELO
        ├── thief-guess/index.ts      # Thief guessing phase
        └── admob-ssv/index.ts        # AdMob ECDSA SSV + coin award
```

---

## Game Modes

| Mode | Arabic | Includes |
|------|--------|----------|
| General | سكرو العامة | All cards (66–68) |
| Classic | سكرو كلاسيك | No Thief, No Ping/Pong |
| Thief | سكرو الحرامي | No Ping/Pong, includes Thief |
| Doubles | سكرو الثنائيات | No Thief, includes Ping/Pong (2v2) |

---

## Architecture Overview

### Server-Authoritative State
All hidden card values are stored in Supabase Postgres (`game_state.draw_pile`, `match_players.hand`).
Clients receive only their own hand data via Row Level Security + Edge Functions.

### Real-time Synchronisation
- **Supabase Realtime Broadcast** – low-latency game events (turn changes, Basra, emojis)
- **Supabase Realtime Presence** – player connection tracking + bot fallback on disconnect
- **pg_cron** – server-tick heartbeat every 5 seconds to expire stale turns

### Basra Concurrency
Concurrent out-of-turn Basra attempts are serialised via a FIFO queue (`game_state.basra_queue`)
with server-generated timestamps, ensuring exactly-once resolution.

### AI (MCTS + DDA)
- **Monte Carlo Tree Search** with determinization for imperfect information
- **Intentional memory decay** – AI forgets swapped card values; probabilistic retention
- **Dynamic Difficulty Adjustment** – adapts AI aggressiveness based on human win rate

### AdMob Server-Side Verification
Client passes `user_id` in `ServerSideVerificationOptions.custom_data`.
Google's servers POST an ECDSA-signed callback to the `admob-ssv` Edge Function, which:
1. Fetches Google's public keys
2. Verifies the P-256 ECDSA signature
3. Issues a coin `UPDATE` via the Supabase admin API

---

## Setup

### Prerequisites
- Unity 2022.3 LTS or later
- Supabase project (free tier sufficient for development)
- NuGetForUnity (for `supabase-csharp`, `UniTask`)
- RTLTMPro plugin (Unity Asset Store)
- DOTween (Unity Asset Store)

### Supabase Setup
```bash
supabase login
supabase link --project-ref <your-project-ref>
supabase db push
supabase functions deploy
```

### Unity Setup
1. Import **NuGetForUnity** from the Package Manager
2. Install `Supabase`, `Cysharp.UniTask`, `Newtonsoft.Json` via NuGet
3. Import **RTLTMPro** from the Asset Store
4. Import **DOTween** from the Asset Store
5. Import **Cinemachine** from the Package Manager
6. Set Supabase URL and Anon Key in the `SupabaseManager` Inspector
7. Set AdMob Ad Unit ID in the `AdMobManager` Inspector

---

## Documentation

| Document | Purpose |
|----------|---------|
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | System architecture with Mermaid diagrams (component, sequence, ER, state, deployment) |
| [`docs/DESIGN_PATTERNS.md`](docs/DESIGN_PATTERNS.md) | Catalogue of design patterns used (Singleton, State, Observer, MCTS, FIFO, etc.) with code refs |
| [`docs/CODEBASE.md`](docs/CODEBASE.md) | Per-namespace, per-class reference documentation |
| [`docs/SOP.md`](docs/SOP.md) | Standard Operating Procedures: dev workflow, releases, incident response, on-call |
| [`docs/FUTURE_PLAN.md`](docs/FUTURE_PLAN.md) | 18-month product roadmap, OKRs, risk register |

---

## Engine Version

**Pinned to Unity 6000.0.32f1 LTS (Unity 6)** via `ProjectSettings/ProjectVersion.txt`.
Open the project with Unity Hub — it will offer to install the matching Editor automatically.

---

## Naming Conventions

| Context | Convention | Example |
|---------|-----------|---------|
| C# classes & methods | PascalCase | `PlayerStateMachine` |
| C# parameters & fields | camelCase | `matchId` |
| Edge Functions & folders | kebab-case | `deck-shuffle` |
| Database tables | snake_case | `match_players` |