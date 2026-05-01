# Agent Instructions — Skrew Unity

## Supabase migrations

When authoring or editing SQL files under `supabase/migrations/`, follow these
project-specific rules. They were learned by running `supabase db reset --linked`
against the managed (cloud) project and hitting failures.

### `supabase/config.toml`

- The Supabase CLI does **not** accept top-level `id`, `name`, or `region` keys.
  Use only `project_id = "..."` for the project identifier. Any other top-level
  metadata must be removed or the CLI fails with
  `'config.config' has invalid keys: id, name, region`.

### SQL conventions for managed Postgres

- **Do not use `uuid_generate_v4()`.** The `uuid-ossp` extension is not exposed
  to the `postgres` role on hosted Supabase. Always use `gen_random_uuid()`
  (provided by `pgcrypto`, which is enabled by default).
- **Do not use psql meta-commands** (`\gexec`, `\i`, `\set`, etc.). Migrations
  are executed through the Supabase migration runner, not `psql`, so backslash
  commands produce `syntax error at or near "\"`. Use a `DO $$ ... $$;` block
  with `EXECUTE format(...)` instead.
- **Cluster/role-level settings are not permitted.** `ALTER SYSTEM`, `ALTER
  ROLE postgres ...`, and similar will fail with
  `permission denied to set parameter`. If a setting must be applied, scope it
  to the current database with `ALTER DATABASE <current> SET ...` and wrap the
  statement in a `DO` block that catches `insufficient_privilege` so the
  migration is idempotent across local and hosted environments:

  ```sql
  DO $$
  BEGIN
      EXECUTE format('ALTER DATABASE %I SET log_statement = %L',
                     current_database(), 'mod');
  EXCEPTION WHEN insufficient_privilege THEN
      RAISE NOTICE 'Skipping: insufficient privilege';
  END
  $$;
  ```

### Workflow

- Link once per machine: `supabase link --project-ref <ref>` from the repo root
  (the CLI auto-detects `supabase/config.toml`).
- Apply pending migrations to the linked remote: `supabase db push`.
- To wipe and re-apply everything on the remote (destructive):
  `supabase db reset --linked`. Pipe `echo y |` to auto-confirm in scripts.
- The warning `no files matched pattern: supabase/seed.sql` is benign — there
  is no local seed file; seed data lives in `004_seed_data.sql`.
