# Architecture — Skrew

> **Audience:** Engineers, architects, and technical reviewers.
> **Last reviewed:** 2026-04-28
> **Diagrams:** Rendered with Mermaid (GitHub-native).

---

## 1. System Overview

Skrew is a **client–server multiplayer card game**. The Unity client is a *thin presentation layer*; **all authoritative game state lives in Supabase Postgres** and all rules are evaluated by **Deno Edge Functions**. Real-time fan-out uses **Supabase Realtime Broadcast** (low-latency events) and **Realtime Presence** (connection tracking).

### High-level Component Diagram

```mermaid
flowchart LR
    subgraph "Client (Unity 6000.3 LTS)"
        UI["UI Layer<br/>(HUD, Grid, Modals)"]
        SM["State Machine<br/>(GameStateMachine)"]
        GL["GameLoop<br/>(partial class)"]
        AI["AI Subsystem<br/>(MCTS + DDA)"]
        L10N["Localization<br/>(en/ar)"]
        SUP["SupabaseManager<br/>(UniTask)"]
    end

    subgraph "Supabase (Backend)"
        AUTH["Auth"]
        PG[("Postgres<br/>(authoritative state)")]
        RT["Realtime<br/>(Broadcast + Presence + Postgres CDC)"]
        EF["Edge Functions<br/>(Deno)"]
        STO["Storage<br/>(cosmetics, avatars)"]
    end

    subgraph "External"
        ADM["Google AdMob<br/>(rewarded ads + SSV callback)"]
        STORE["App Stores<br/>(Apple, Google, web)"]
        OBS["Observability<br/>(Grafana, Sentry)"]
    end

    UI --> SM
    SM --> GL
    GL --> SUP
    AI --> GL
    L10N --> UI

    SUP <--> AUTH
    SUP <--> RT
    SUP --> EF
    EF --> PG
    PG -- "CDC" --> RT
    EF --> RT
    SUP --> STO

    ADM -- "ECDSA-signed POST" --> EF
    STORE --> UI
    EF -.-> OBS
    PG -.-> OBS
```

---

## 2. Layer Responsibilities

| Layer | Path | Responsibility |
|-------|------|----------------|
| **UI** | `Assets/Scripts/UI/` | Render game state; capture user input; **never** mutate authoritative state. |
| **State Machine** | `Assets/Scripts/StateMachines/` | Phase transitions; handler interfaces (Basra, Thief, Ping/Pong). |
| **Game Loop** | `Assets/Scripts/Core/GameLoop*.cs` | Single entry point for Realtime events; thin façade over Edge Function calls. |
| **AI** | `Assets/Scripts/AI/` | MCTS + intentional memory decay + DDA; runs both online (disconnect bots) and offline (vs. CPU). |
| **Networking** | `Assets/Scripts/Networking/` | Presence, ephemeral broadcast (emojis, stickers). |
| **Social** | `Assets/Scripts/Social/` | Persistent chat backed by Postgres + CDC. |
| **Gamification** | `Assets/Scripts/Gamification/` | Streaks, missions, leaderboard, AdMob client wrapper. |
| **Localization** | `Assets/Scripts/Localization/` | JSON bundle loader + RTLTMPro adapter. |
| **Edge Functions** | `supabase/functions/` | Authoritative rule engine: shuffle, draw, swap, Basra, scoring, SSV. |
| **Migrations** | `supabase/migrations/` | Versioned schema, RLS, cron jobs, RPCs. |

**Strict rule:** *Clients never receive opponents' face-down card values.* The `game_state` table has RLS that blocks **all** client reads (`USING (FALSE)`); hand info is delivered exclusively via Edge Functions or via personalised Realtime Broadcast payloads.

---

## 3. Match Lifecycle (Sequence Diagram)

```mermaid
sequenceDiagram
    autonumber
    actor P1 as Player 1 (Unity)
    actor P2 as Player 2 (Unity)
    participant EF as Edge Functions
    participant DB as Postgres
    participant RT as Realtime
    participant Cron as pg_cron

    Note over P1,P2: 1. Lobby & match creation
    P1->>EF: POST /match-create (mode=Classic)
    EF->>DB: INSERT matches, match_players
    EF-->>P1: match_id
    P2->>EF: POST /match-join (match_id)
    EF->>DB: UPSERT match_players
    EF->>RT: broadcast match:* match_started

    Note over P1,P2: 2. Deal & Info Phase
    EF->>EF: deck-shuffle (CSPRNG Fisher-Yates)
    EF->>DB: UPDATE game_state SET draw_pile, hands, discard_top
    EF->>RT: broadcast info_phase_start (duration=8s)
    P1->>P1: Reveal indices 2,3 for 8s
    P2->>P2: Reveal indices 2,3 for 8s

    Note over P1,P2: 3. Turn loop
    Cron->>EF: invoke game-tick (5s)
    EF->>DB: SELECT expired turns
    alt Turn expired
      EF->>RT: broadcast turn_changed (next_seat)
    end
    P1->>EF: POST /draw-from-stock
    EF->>DB: pop draw_pile → push to P1's hand (private)
    EF-->>P1: drawn_card (private)
    P1->>EF: POST /swap-drawn-card (index=2)
    EF->>DB: UPDATE hand[2], discard_pile
    EF->>RT: broadcast card_swapped (public: index=2 only)

    Note over P1,P2: 4. Out-of-turn Basra (race)
    EF->>RT: broadcast discard_event (top=7)
    par Concurrent slap
      P1->>EF: POST /basra-resolve (card_index=1, ts=…)
      P2->>EF: POST /basra-resolve (card_index=0, ts=…)
    end
    EF->>DB: INSERT basra_queue (FIFO by server ts)
    EF->>EF: pop earliest, validate match
    EF->>RT: broadcast basra_success (winner_id)

    Note over P1,P2: 5. Screw declaration & endgame
    P1->>EF: POST /declare-screw
    EF->>DB: SET screw_caller_id, lock P1
    EF->>RT: broadcast screw_called
    loop Final orbit
      EF->>RT: broadcast turn_changed
      P2->>EF: POST /draw-from-stock or /declare-screw
    end
    EF->>EF: score-calc (x2 penalty if caller failed)
    alt Thief mode active
      EF->>RT: broadcast thief_guess_required
      P1->>EF: POST /thief-guess (guessed_player_id)
      EF->>EF: swap scores if wrong
    end
    EF->>DB: UPDATE users.elo_rating, total_wins
    EF->>RT: broadcast match_ended (scores, winner)
```

---

## 4. Database ER Diagram

```mermaid
erDiagram
    users ||--o{ match_players : "joins"
    users ||--o{ user_missions : "tracks"
    users ||--o{ user_badges : "earns"
    users ||--o{ messages : "sends"
    users ||--o{ coin_transactions : "receives"
    matches ||--|{ match_players : "has"
    matches ||--|| game_state : "owns"
    matches ||--o{ messages : "scopes"
    missions ||--o{ user_missions : "instantiated"
    badges ||--o{ user_badges : "awarded"
    cosmetics }o--o{ users : "owns (via owned_cosmetics)"

    users {
        UUID id PK
        UUID auth_id FK
        TEXT username UK
        TEXT display_name
        INT coins
        INT elo_rating
        INT total_wins
        INT total_losses
        INT streak_count
        DATE last_login_date
        TIMESTAMPTZ created_at
    }
    matches {
        UUID id PK
        TEXT mode
        TEXT status
        INT current_seat
        UUID screw_caller_id FK
        TIMESTAMPTZ created_at
        TIMESTAMPTZ ended_at
    }
    match_players {
        UUID id PK
        UUID match_id FK
        UUID user_id FK
        INT seat
        JSONB hand "private; RLS-blocked"
        INT final_score
    }
    game_state {
        UUID match_id PK,FK "no client reads"
        JSONB draw_pile
        JSONB discard_pile
        JSONB basra_queue
        JSONB action_log
        TIMESTAMPTZ turn_started_at
    }
    messages {
        UUID id PK
        UUID match_id FK
        UUID user_id FK
        TEXT content
        TIMESTAMPTZ created_at
    }
    missions {
        UUID id PK
        TEXT title_en
        TEXT title_ar
        TEXT period "daily|weekly"
        INT target_count
        INT coin_reward
        UUID badge_id FK
    }
    user_missions {
        UUID id PK
        UUID user_id FK
        UUID mission_id FK
        DATE period_start
        INT progress
        BOOL completed
        TIMESTAMPTZ completed_at
    }
    badges {
        UUID id PK
        TEXT key UK
        TEXT title_en
        TEXT title_ar
        TEXT icon_url
    }
    user_badges {
        UUID id PK
        UUID user_id FK
        UUID badge_id FK
        TIMESTAMPTZ earned_at
    }
    cosmetics {
        UUID id PK
        TEXT type "card_back|table_theme|avatar"
        TEXT key UK
        INT coin_price
        TEXT asset_url
    }
    coin_transactions {
        UUID id PK
        UUID user_id FK
        TEXT transaction_id UK "AdMob dedup"
        INT coins
        TEXT source
        TIMESTAMPTZ created_at
    }
```

---

## 5. Client State Machine

```mermaid
stateDiagram-v2
    [*] --> Lobby
    Lobby --> InfoPhase : match_started
    InfoPhase --> PlayerTurn : info_phase_expired
    PlayerTurn --> ResolveAction : command_card_played
    ResolveAction --> PlayerTurn : effect_resolved
    PlayerTurn --> PlayerTurn : turn_changed
    PlayerTurn --> MatchEnd : screw_called / obligatory_screw
    MatchEnd --> ThiefGuess : thief_active
    ThiefGuess --> Reveal : guess_submitted
    MatchEnd --> Reveal : non_thief_mode
    Reveal --> Lobby : new_match
    Reveal --> [*] : exit

    note right of PlayerTurn
        Always-on Basra handler:
        IBasraHandler reacts to
        Realtime discard_event
        regardless of active seat.
    end note
```

---

## 6. AI Pipeline

```mermaid
flowchart TD
    A["Turn assigned to bot"] --> B{Difficulty < 0.2?}
    B -- "yes (easy)" --> R["Random legal action"]
    B -- "no" --> C["Build determinized<br/>AiGameState clone"]
    C --> D["MCTS root<br/>(N iterations ∝ difficulty)"]

    subgraph MCTS Iteration
        S["Selection<br/>(UCB1)"] --> E["Expansion<br/>(untried action)"]
        E --> SIM["Simulation<br/>(random rollout)"]
        SIM --> BP["Backpropagation"]
    end

    D --> S
    BP --> D
    D --> CHOOSE["Pick child<br/>with most visits"]
    CHOOSE --> EXEC["Execute action via<br/>GameLoop Edge Function call"]
    R --> EXEC

    EXEC --> MEM["AiMemoryMatrix.ApplyDecay()<br/>(intentional forgetting)"]
    MEM --> END["End turn"]

    DDA["DDA — AdjustDifficulty(humanScoreDelta)"] -.->|adjusts retention<br/>and iterations| D
```

**Key invariants:**
- **Intentional forgetting:** memory cleared on blind swap; probabilistic decay each turn (`retentionRate ∈ [0.4, 1.0]`, scaled by difficulty).
- **Determinization:** unknown opponent cards are sampled from the remaining-deck distribution before each MCTS rollout (imperfect information).
- **DDA bounds:** difficulty ∈ [0, 1], step = `clamp(humanScoreDelta * 0.02, -0.15, 0.15)` per round.

---

## 7. Real-Time Concurrency — Basra FIFO

```mermaid
sequenceDiagram
    participant C1 as Client 1
    participant C2 as Client 2
    participant EF as basra-resolve
    participant DB as game_state.basra_queue

    Note over C1,C2: discard_event broadcast — top card = 7
    par Race
        C1->>EF: attempt (card_index=1, ts=1714,300,001ms)
        C2->>EF: attempt (card_index=0, ts=1714,300,003ms)
    end
    EF->>DB: INSERT (server_ts := NOW(), payload)
    EF->>DB: SELECT … ORDER BY server_ts ASC LIMIT 1<br/>FOR UPDATE SKIP LOCKED
    DB-->>EF: winner = C1 (earlier ts)
    alt Card matches discard top
        EF->>DB: remove from C1 hand, push to discard
        EF-->>RT: broadcast basra_success (C1)
        EF-->>RT: broadcast basra_rejected (C2 — too late)
    else No match
        EF->>DB: keep card + take discard top into hand
        EF-->>RT: broadcast basra_failed (C1, +10 penalty)
    end
```

The `FOR UPDATE SKIP LOCKED` pattern ensures **exactly-once** processing under concurrent invocations.

---

## 8. Deployment Topology

```mermaid
flowchart TB
    subgraph Devices
        iOS["iOS (TestFlight + App Store)"]
        AND["Android (Play Console)"]
        WEB["WebGL (CloudFront CDN)"]
    end

    subgraph "Supabase (us-east-1 primary, eu-west-1 read replica)"
        ED["Edge Functions Pool"]
        PGP[("Postgres Primary")]
        PGR[("Postgres Read Replica")]
        RTC["Realtime Cluster"]
        STG["Storage (S3-backed)"]
    end

    subgraph CI/CD
        GHA["GitHub Actions"]
        UCB["Unity Cloud Build"]
    end

    subgraph Observability
        SEN["Sentry"]
        GRA["Grafana"]
        SLO["Supabase Logs"]
    end

    iOS --> ED
    AND --> ED
    WEB --> ED
    ED --> PGP
    ED --> PGR
    ED <--> RTC
    ED --> STG

    GHA --> ED
    GHA --> PGP
    UCB --> iOS
    UCB --> AND
    UCB --> WEB

    iOS -.-> SEN
    AND -.-> SEN
    WEB -.-> SEN
    ED -.-> SLO
    PGP -.-> SLO
    SLO --> GRA
```

---

## 9. Threat Model (Abridged)

| Threat | Vector | Mitigation |
|--------|--------|------------|
| Cheat: peek opponent's hidden hand | Client memory inspection | Server-authoritative; `game_state` RLS = `FALSE` for clients |
| Cheat: force "Screw" with wrong score | Crafted Edge Function payload | Edge Function recomputes scores from server-side hand |
| Fraud: replay AdMob SSV callback | Replay attack | `coin_transactions.transaction_id UNIQUE` idempotency |
| Spam: flood Realtime channel | Malicious client | Channel rate-limit (10 msg/s); ban list |
| Privacy: leak chat to non-members | Mis-scoped RLS | `messages` RLS scoped by `match_id` membership |
| Account theft | Stolen JWT | 1 h JWT lifetime; refresh tokens revocable; 2FA in roadmap |

---

## 10. Quality Attributes

| Attribute | Target | Measured by |
|-----------|--------|-------------|
| **Latency** | p95 ≤ 350 ms turn-action round-trip | Edge Function logs + client timing |
| **Throughput** | 5 000 concurrent matches per region | Load test in CI |
| **Availability** | 99.9% monthly | Supabase status + uptime probe |
| **Crash-free** | ≥ 99.3% sessions | Sentry |
| **Determinism** | Repro of any reported match from `match_actions` | Server-side replay tool |
| **Accessibility** | WCAG 2.1 AA on UI text | Manual + automated audits |

---

## 11. Open Architectural Questions

1. **CRDT vs server-authoritative for chat history?** — Currently server-authoritative; consider a Y.js layer if collaborative editing is added.
2. **Move to Cloudflare Workers** — sub-50 ms edge globally; trade-off: lose Supabase's typed SDK convenience.
3. **Deterministic rollback netcode** for tournaments — required for esports parity.
4. **Avatar pipeline** — Ready Player Me vs in-house?

These are tracked in `FUTURE_PLAN.md` and reviewed each quarter.
