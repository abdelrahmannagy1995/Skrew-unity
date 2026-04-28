# Future Plan / Product Roadmap — Skrew

> **Horizon:** 18 months (May 2026 → Nov 2027)
> **North-star metric:** Day-30 retention ≥ 22% on the Classic Screw mode.
> **Owner:** Product + Engineering leads. Reviewed quarterly.

---

## Vision

Become the **definitive global digital home of the Egyptian Screw card game**, expanding from a regional MENA cult favourite to a world-class strategy card-game franchise with esports-grade competition, cross-platform parity, and a rich social fabric.

---

## Strategic Pillars

1. **Authentic gameplay** — Honour the colloquial Arabic ruleset (Basra, Thief, Ping/Pong) while making it learnable for non-Arabic speakers.
2. **Server-authoritative trust** — All hidden state lives in Supabase; clients are presentation layers.
3. **Cosmetic-only monetization** — No pay-to-win. Revenue comes from card backs, table themes, animated avatars, and rewarded ads.
4. **AI that feels human** — MCTS + intentional forgetting + DDA. Players must never feel cheated.
5. **Community first** — In-game chat, clubs, tournaments, and creator features.

---

## Roadmap

### Q2 2026 — **Launch & Stabilisation** (v1.0 → v1.2)
| Theme | Deliverable |
|-------|-------------|
| Platforms | Android + iOS soft launch in Egypt, Saudi Arabia, UAE; WebGL beta on `play.skrew.app` |
| Modes | All four modes (General, Classic, Thief, Doubles) shipped |
| AI | Single-player vs. CPU at 3 difficulty tiers; DDA for online disconnect bots |
| Monetization | Rewarded ads (AdMob SSV), 5 starter card-back cosmetics |
| Telemetry | Sentry crash reporting, Supabase Logs, Grafana board for LiveOps |

**Success criteria:** Crash-free sessions ≥ 99.3%, p95 turn-action latency ≤ 350 ms, Day-7 retention ≥ 30%.

### Q3 2026 — **Social Layer** (v1.3 → v1.5)
| Theme | Deliverable |
|-------|-------------|
| Friends | Add/remove friends, friend invitations to private rooms |
| Clubs | "Diwan" clubs (max 50 members) with private chat + weekly inter-club challenges |
| Chat | Voice notes (push-to-talk) using Supabase Storage for ephemeral 7-day blobs |
| Stickers | Animated sticker packs (Eid, Ramadan, World Cup themed) |
| Localization | Add **French**, **Levantine Arabic**, and **Gulf Arabic** dialects |

### Q4 2026 — **Competitive Esports** (v1.6 → v2.0)
| Theme | Deliverable |
|-------|-------------|
| Ranked | True ELO ladder for Classic mode with seasonal resets, 10 tiers (Bronze → Mythic) |
| Tournaments | Daily 32-player single-elimination brackets with coin prize pools |
| Replays | Server-recorded match replays via append-only `match_actions` table; share via deep link |
| Spectator | Live-spectate top-20 players with 30 s anti-cheat delay |
| Streaming | Twitch/YouTube broadcast API for casters |

### Q1 2027 — **Content & Personalisation** (v2.1 → v2.3)
| Theme | Deliverable |
|-------|-------------|
| Avatars | 3D avatar system with hats, glasses, beards, palette swaps |
| Battle Pass | "Wantana" seasonal pass, 60 tiers, free + premium tracks |
| Themes | Animated table themes (café, dahabiya, oasis, Cairo rooftop) |
| Sound | Adaptive music engine that intensifies as Screw is approached |
| Accessibility | Colour-blind mode, screen-reader hooks, dyslexia-friendly font |

### Q2 2027 — **AI Coaching** (v2.4 → v2.5)
| Theme | Deliverable |
|-------|-------------|
| Coach | Post-match AI coach reviews missed Basra opportunities and risky Screw calls |
| Drills | Daily memory & probability drills (single-player puzzles) |
| Personality | Themed AI opponents with backstories ("El Asta", "Tante Faten") |
| LLM Integration | Optional GPT-style assistant for rule explanations in any language |

### Q3 2027 — **Platform Expansion** (v2.6 → v3.0)
| Theme | Deliverable |
|-------|-------------|
| Steam | Native Windows + macOS + Linux desktop client |
| Smart TV | Android TV / Apple TV companion with phone-as-controller |
| Apple Vision | Spatial-computing prototype (mixed-reality table) |
| Cross-progression | Single account across all platforms; cloud-saved cosmetics |

---

## Backlog (Unscheduled, Priority Pool)

### Engineering
- Migrate Edge Functions from Deno to **Cloudflare Workers** for sub-50 ms global latency.
- Adopt **WebTransport** when Unity 6.x supports it natively, replacing WebSockets for lower jitter.
- **Photon Quantum**-style deterministic rollback for tournament-grade fairness.
- Open-source the AI agent (`ScrewAiAgent`) under MIT to attract academic research.
- **Edge-cached deck shuffles**: pre-generate batches of CSPRNG decks in a Postgres table to remove per-match randomness latency.

### Game Design
- **Custom rule editor** — let clubs tweak Basra penalty, info-phase duration, or remove specific commands.
- **Daily puzzle mode** — single-player "what's the optimal action?" puzzles, leaderboard scored.
- **Co-op campaign** — 4-player PvE: bots scale up over 12 levels.
- **Asynchronous Screw** — turn-based async matches (like Words With Friends) for casual players.

### Monetization
- **Subscription tier** "Skrew+" (USD 4.99/month) — ad-free, double daily-streak coins, exclusive monthly card back.
- **Battle Pass** seasonal at USD 9.99 with 60 tiers.
- **Localized payment rails** — Fawry, STC Pay, Apple Pay, Google Pay.
- **Influencer codes** — gift codes generated for content creators tracked via the `gift_codes` table.

### Community
- **Creator program** — revenue share for stream/clip creators with > 10 k followers.
- **Localised content** — Ramadan tournaments, Eid card backs, Egyptian cup branding.
- **Charity bracket** — quarterly pro-am benefiting MENA literacy charities.

---

## Technical Debt Watchlist

| Item | Severity | Target |
|------|----------|--------|
| `GameLoop` is split into a partial class with growing surface — refactor into mediator + command bus | High | Q3 2026 |
| `OpponentGridManager`, `ScoreboardUI`, `ThiefGuessModal` are stubs needing UX polish | High | Q2 2026 |
| `ScrewAiAgent.Simulate()` rollout uses uniform random — upgrade to a learned policy network | Medium | Q1 2027 |
| RTL plugin loaded via reflection — replace with hard dependency once RTLTMPro `package.json` is published | Low | Q2 2027 |
| No integration tests against live Supabase yet — add a Pulumi-managed staging project + Playwright suite | High | Q3 2026 |

---

## Risk Register

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Apple/Google policy shift on real-money gameplay perception | Medium | High | Cosmetic-only monetization; clear EULA; legal review per quarter |
| AdMob SSV public-key rotation breaking verification | Low | Medium | Cache keys with 24 h TTL; alert on signature failures > 0.5% |
| Server cost spike from Realtime Broadcast at scale | Medium | High | Provision auto-scaling read replicas; rate-limit emoji spam |
| Anti-cheat bypass via reverse-engineered client | Medium | High | Server-authoritative, encrypted Anon-key, periodic external pentests |
| RTL rendering regressions on Unity TMP updates | Medium | Medium | Pin RTLTMPro version; visual diff tests in CI |
| Cultural mistranslation of card names ("Khod w Hat") | Low | High | Native MENA QA reviewer signs off every locale change |

---

## OKRs Snapshot — H2 2026

**Objective 1 — Make Skrew the most played card game in MENA by Dec 2026**
- KR1: 2.5 M MAU across MENA region.
- KR2: Day-30 retention ≥ 22% on Classic mode.
- KR3: Top-5 Free Card Game in 7 MENA app stores.

**Objective 2 — Engineering excellence**
- KR1: Crash-free sessions ≥ 99.5%.
- KR2: p95 turn-action latency ≤ 300 ms globally.
- KR3: ≥ 70% line coverage on Core/AI/Gameplay namespaces.

**Objective 3 — Sustainable monetization**
- KR1: ARPDAU ≥ USD 0.07 in MENA, USD 0.20 in EU/NA.
- KR2: Rewarded-ad opt-in rate ≥ 35% of DAU.
- KR3: Cosmetic conversion rate ≥ 4% of MAU.

---

## Decision Log

| Date | Decision | Rationale | Reviewed |
|------|----------|-----------|----------|
| 2026-04-15 | Pinned engine to Unity 6000.3.14f1 LTS | Recommended LTS release; two-year support window; first LTS with native iOS 18 support | Engineering lead |
| 2026-04-20 | Chose Supabase over Firebase | Postgres power + Realtime + RLS + Edge Functions in one stack | CTO |
| 2026-04-22 | MCTS over deep RL for AI v1 | Explainable behaviour; deterministic; cheap to run on-device | AI lead |
| 2026-04-28 | Cosmetic-only monetization | Aligns with Apple/Google policy + community trust | Product + Legal |

---

**Updates** to this roadmap require a PR labelled `docs/roadmap` reviewed by the Product and Engineering leads.
