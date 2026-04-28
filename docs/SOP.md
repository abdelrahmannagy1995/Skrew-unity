# Standard Operating Procedures (SOP) — Skrew

> **Scope:** Day-to-day workflow, release management, incident response, and on-call rotations for the Skrew (سكرو) cross-platform card game.
> **Audience:** All engineers, designers, QA, ops, and on-call staff.
> **Last reviewed:** 2026-04-28

---

## 1. Engineering Workflow

### 1.1 Branching Model — Trunk-Based with Short-Lived Feature Branches
| Branch | Purpose | Lifetime |
|--------|---------|----------|
| `main` | Always-deployable trunk; protected; merge via PR only | Permanent |
| `release/<version>` | Release-candidate stabilisation (e.g., `release/1.4.0`) | ≤ 2 weeks |
| `feature/<scope>-<short-desc>` | Single-developer feature branches | ≤ 5 days |
| `hotfix/<jira-id>-<desc>` | Production hotfixes branched off the active `release/*` tag | ≤ 24 h |
| `copilot/<task>` | Agent-driven implementation branches | Open until merged |

**Rules**
1. No direct commits to `main`.
2. PRs require **2 approvals** (1 engineer + 1 domain owner) and **all CI green**.
3. Squash-merge by default; preserve merge commits only when integrating a `release/*` branch back to `main`.
4. Long-running branches (> 5 days) **must** rebase on `main` daily.

### 1.2 Local Setup Checklist
1. Install **Unity 6000.0.32f1 LTS** via Unity Hub (the version is pinned in `ProjectSettings/ProjectVersion.txt`).
2. Install **NuGetForUnity** via the Package Manager (already declared in `Packages/manifest.json`).
3. Restore NuGet packages: `Window → NuGet → Restore Packages`. Required packages:
   - `Supabase` (≥ 0.16)
   - `Cysharp.UniTask` (≥ 2.5.10)
   - `Newtonsoft.Json` (Unity package, already in manifest)
   - `Google.Apis.Auth` (for AdMob SSV verification helpers)
4. Import the **RTLTMPro** plugin from the Asset Store.
5. Import **DOTween (HOTween v2)** from the Asset Store.
6. Configure local Supabase secrets in `Assets/Resources/SupabaseConfig.asset` (a `ScriptableObject` — never commit real keys).
7. Run the **EditMode** test runner (`Window → General → Test Runner`) and verify all green before committing.

### 1.3 Code Review Standards
- **Style:** PascalCase for C# types/methods; camelCase for parameters/locals; kebab-case for Edge Functions and folders. Enforced by `Assets/csc.rsp` (warnings as errors except `CS0618`).
- **Test coverage:** New features require ≥ 70% line coverage on Core/AI/Gameplay namespaces.
- **PR checklist** (must be ticked):
  - [ ] Public API documented with `<summary>` XML comments.
  - [ ] No raw Supabase service-role keys in client code.
  - [ ] No new `Singleton` without scene-lifecycle justification.
  - [ ] Localization strings added for both `en.json` and `ar.json` if UI text changed.
  - [ ] DOTween animations released or killed in `OnDestroy()`.

---

## 2. Release Management

### 2.1 Release Cadence
| Cadence | Type | Channels |
|---------|------|----------|
| Bi-weekly (Tue) | Server-side Edge Function deploy | Production Supabase |
| Monthly | Client app release | Google Play, Apple App Store, WebGL CDN |
| As needed | Hotfix | All channels within 24 h |

### 2.2 Versioning — Semantic Versioning 2.0.0
- `MAJOR.MINOR.PATCH` (e.g., `1.4.2`)
- **MAJOR**: Breaking server-protocol or save-format changes.
- **MINOR**: New game modes, AI features, cosmetics, missions.
- **PATCH**: Bug fixes, balance tweaks, localization corrections.
- Build number = `YYYYMMDD.<count>` (e.g., `20260428.1`) injected into `Application.version` via CI.

### 2.3 Release Steps
1. **Cut** `release/<version>` from `main`.
2. **Bump** `ProjectSettings/ProjectSettings.asset` `bundleVersion` and `Android.bundleVersionCode` / `iOS.buildNumber`.
3. **Update** `CHANGELOG.md` (Keep-A-Changelog format).
4. **Smoke test** all four game modes on each platform (iOS, Android, WebGL).
5. **Deploy backend first**: `supabase db push && supabase functions deploy`.
6. **Tag** the release: `git tag -s v1.4.0 -m "Release 1.4.0"`.
7. **Build** clients via the CI pipeline (`build-android.yml`, `build-ios.yml`, `build-webgl.yml`).
8. **Submit** mobile builds to stores (24–72 h review SLA).
9. **Monitor** Sentry/Supabase logs for the first 6 h after rollout.

### 2.4 Rollback Procedure
- **Edge Functions:** Re-deploy the previous tag (`supabase functions deploy --version v1.3.x`).
- **Database migrations:** Roll forward only — write a compensating migration; never run a destructive `down`.
- **Mobile clients:** Halt staged rollout in Play Console / App Store Connect; submit a hotfix build.
- **WebGL:** CloudFront invalidation back to previous CDN folder (`/builds/v1.3.x/`).

---

## 3. Incident Response

### 3.1 Severity Levels
| Sev | Definition | Response | Comms |
|-----|------------|----------|-------|
| **S1** | Service down or > 5% of matches failing | < 15 min ack, < 1 h mitigation | Status page + Discord announce |
| **S2** | Major feature broken (Basra / Screw call) | < 30 min ack, < 4 h mitigation | Discord announce |
| **S3** | Localization or cosmetic bug | Next business day | Internal only |
| **S4** | Single-user report | Backlog triage | Support response |

### 3.2 On-Call Rotation
- **Primary on-call:** 1 engineer per week, Mon 09:00 → Mon 09:00 UTC.
- **Secondary on-call:** Backup engineer covering same window.
- **Escalation chain:** Primary → Secondary → Engineering Lead → CTO.
- **Pager:** PagerDuty integration with Supabase Logs + Sentry alerts.

### 3.3 Runbook Index
- `runbooks/edge-function-down.md`
- `runbooks/realtime-channel-saturated.md`
- `runbooks/admob-ssv-failures.md`
- `runbooks/postgres-cpu-spike.md`
- `runbooks/desync-detected.md`

### 3.4 Post-Incident Review (Blameless)
Within 5 business days of every S1/S2:
1. Timeline reconstruction.
2. Root cause (5 Whys).
3. Impact quantification (affected users × duration × % matches).
4. Action items with named owners and due dates.
5. Publish to `docs/postmortems/YYYY-MM-DD-<slug>.md`.

---

## 4. Customer Support

### 4.1 Tiering
- **Tier 1:** General gameplay questions, account recovery — handled by support staff via the admin dashboard.
- **Tier 2:** Suspected bugs, missing rewards — escalated to a duty engineer.
- **Tier 3:** Security or fraud (multi-account, ad-spoofing) — escalated to Trust & Safety lead.

### 4.2 Admin Dashboard Usage
- **All** admin queries inject metadata comments: `-- user: <staff_email> -- source: admin_dashboard -- ticket: <id>`.
- **Postgres** is configured (migration `001`) with `log_statement = 'mod'` at the database level so all `INSERT/UPDATE/DELETE` are auditable.
- **Coin grants** must use the `award_coins(p_user_id, p_coins, p_transaction_id, p_source)` RPC for idempotency — never raw `UPDATE`.

---

## 5. Security & Compliance

### 5.1 Secrets Management
- **Production secrets** live only in Supabase project secrets and the CI vault (GitHub Actions encrypted secrets).
- **Local development** uses `.env.local` (git-ignored) and a local Supabase CLI instance.
- **Rotation:** Anon key — never; Service role key — every 90 days; AdMob server-key — every 180 days.

### 5.2 Privacy
- Only `display_name`, `coins`, `elo_rating`, and aggregate stats are publicly queryable.
- Chat messages older than 30 days are auto-purged via a daily `pg_cron` job.
- GDPR / CCPA deletion requests are handled via the `delete-user-data` Edge Function (right-to-be-forgotten).

### 5.3 Anti-Cheat
- **Server-authoritative state**: hidden grid arrays never leave Postgres in plaintext to other clients.
- **Rate limits**: 10 actions/sec per player at the Edge Function gateway.
- **AdMob SSV**: ECDSA P-256 signature verification mandatory; coin awards are idempotent via `coin_transactions.transaction_id` UNIQUE constraint.
- **Anomaly detection**: Daily query for users with > 95th percentile win rate triggers manual review.

---

## 6. Telemetry & Observability

### 6.1 Required Logs
| Source | Destination | Retention |
|--------|-------------|-----------|
| Supabase Auth audit | `auth.audit_log_entries` | 365 days |
| Edge Function stdout/stderr | Supabase Logs | 30 days |
| Postgres `log_statement='mod'` | Supabase Logs | 90 days |
| Unity client crashes | Sentry | 90 days |
| Real-time WebSocket metrics | OpenTelemetry → Grafana | 30 days |

### 6.2 Key Dashboards
- **LiveOps:** concurrent matches, MAU/DAU, ARPU, crash-free rate.
- **Game Health:** average turn duration, Basra success rate, Screw success rate, Thief catch rate per mode.
- **Infra:** Edge Function p95 latency, Postgres connections, Realtime channel count.

### 6.3 Alert Thresholds
- Edge Function p95 > 800 ms for 5 min → Sev2 page.
- Realtime channel disconnects > 1% for 10 min → Sev2 page.
- Crash-free sessions < 99.0% over 1 h → Sev2 page.
- AdMob SSV failure rate > 0.5% → Sev3 ticket.

---

## 7. Documentation Hygiene

- Every PR that changes architecture must update `docs/ARCHITECTURE.md`.
- Every new design pattern usage must be entered in `docs/DESIGN_PATTERNS.md`.
- README quick-start must be smoke-tested every release on a fresh machine.
- Documentation review is part of the quarterly tech-debt sprint.

---

## 8. Useful Commands

```bash
# Apply latest migrations to staging
supabase db push --db-url "$STAGING_DB_URL"

# Deploy a single Edge Function
supabase functions deploy basra-resolve --project-ref "$STAGING_PROJECT_REF"

# Tail Edge Function logs
supabase functions logs admob-ssv --tail

# Run Unity headless tests (CI)
"$UNITY" -batchmode -nographics -projectPath . -runTests \
  -testPlatform EditMode -logFile - -quit

# Build WebGL headless (CI)
"$UNITY" -batchmode -nographics -projectPath . \
  -executeMethod Skrew.Build.WebGLBuilder.PerformBuild -quit
```

---

**End of SOP.** Updates require a PR labelled `docs/sop` and the approval of the engineering lead.
