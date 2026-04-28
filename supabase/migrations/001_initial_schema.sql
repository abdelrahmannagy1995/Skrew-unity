-- =============================================================================
-- Migration 001: Initial Schema for Screw Card Game
-- =============================================================================

-- Enable necessary extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "pg_cron";

-- =============================================================================
-- USERS TABLE
-- =============================================================================
CREATE TABLE IF NOT EXISTS public.users (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    auth_id         UUID UNIQUE NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    username        TEXT NOT NULL UNIQUE,
    display_name    TEXT,
    avatar_url      TEXT,
    coins           INTEGER NOT NULL DEFAULT 100,
    total_wins      INTEGER NOT NULL DEFAULT 0,
    total_losses    INTEGER NOT NULL DEFAULT 0,
    elo_rating      INTEGER NOT NULL DEFAULT 1000,
    streak_count    INTEGER NOT NULL DEFAULT 0,
    last_login_date DATE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- MATCHES TABLE
-- =============================================================================
CREATE TYPE public.game_mode AS ENUM (
    'general',    -- General Screw  (سكرو العامة)
    'classic',    -- Classic Screw  (سكرو كلاسيك)
    'thief',      -- Thief Screw    (سكرو الحرامي)
    'doubles'     -- Doubles Screw  (سكرو الثنائيات)
);

CREATE TYPE public.match_status AS ENUM (
    'waiting',
    'info_phase',
    'in_progress',
    'endgame',
    'completed'
);

CREATE TABLE IF NOT EXISTS public.matches (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    game_mode           public.game_mode NOT NULL DEFAULT 'general',
    status              public.match_status NOT NULL DEFAULT 'waiting',
    host_user_id        UUID NOT NULL REFERENCES public.users(id),
    current_turn_index  INTEGER NOT NULL DEFAULT 0,
    turn_started_at     TIMESTAMPTZ,
    turn_timeout_secs   INTEGER NOT NULL DEFAULT 30,
    screw_caller_id     UUID REFERENCES public.users(id),
    final_orbit_done    BOOLEAN NOT NULL DEFAULT FALSE,
    discard_top_card    JSONB,
    discard_pile        JSONB NOT NULL DEFAULT '[]',
    draw_pile_count     INTEGER NOT NULL DEFAULT 0,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- MATCH PLAYERS TABLE (player seats per match)
-- =============================================================================
CREATE TABLE IF NOT EXISTS public.match_players (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    match_id        UUID NOT NULL REFERENCES public.matches(id) ON DELETE CASCADE,
    user_id         UUID NOT NULL REFERENCES public.users(id),
    seat_index      INTEGER NOT NULL,             -- 0-5 seat order
    team            INTEGER,                      -- 0 or 1 for Doubles mode
    hand            JSONB NOT NULL DEFAULT '[]',  -- array of card objects (server-only)
    hand_size       INTEGER NOT NULL DEFAULT 4,
    final_score     INTEGER,
    is_screwed      BOOLEAN NOT NULL DEFAULT FALSE,  -- locked after Screw call
    has_thief       BOOLEAN NOT NULL DEFAULT FALSE,
    is_connected    BOOLEAN NOT NULL DEFAULT TRUE,
    is_bot          BOOLEAN NOT NULL DEFAULT FALSE,
    joined_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (match_id, seat_index),
    UNIQUE (match_id, user_id)
);

-- =============================================================================
-- GAME STATE TABLE (server-authoritative hidden state)
-- =============================================================================
CREATE TABLE IF NOT EXISTS public.game_state (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    match_id        UUID NOT NULL UNIQUE REFERENCES public.matches(id) ON DELETE CASCADE,
    deck            JSONB NOT NULL DEFAULT '[]',   -- full shuffled deck
    draw_pile       JSONB NOT NULL DEFAULT '[]',   -- remaining draw pile
    basra_queue     JSONB NOT NULL DEFAULT '[]',   -- FIFO queue for concurrent Basra attempts
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- MESSAGES TABLE (persistent lobby chat)
-- =============================================================================
CREATE TABLE IF NOT EXISTS public.messages (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    match_id    UUID NOT NULL REFERENCES public.matches(id) ON DELETE CASCADE,
    user_id     UUID NOT NULL REFERENCES public.users(id),
    content     TEXT NOT NULL CHECK (char_length(content) <= 500),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- MISSIONS TABLE
-- =============================================================================
CREATE TYPE public.mission_period AS ENUM ('daily', 'weekly');

CREATE TABLE IF NOT EXISTS public.missions (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title_en        TEXT NOT NULL,
    title_ar        TEXT NOT NULL,
    description_en  TEXT NOT NULL,
    description_ar  TEXT NOT NULL,
    period          public.mission_period NOT NULL,
    target_count    INTEGER NOT NULL DEFAULT 1,
    coin_reward     INTEGER NOT NULL DEFAULT 50,
    badge_id        UUID,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- USER MISSIONS TABLE (progress tracking)
-- =============================================================================
CREATE TABLE IF NOT EXISTS public.user_missions (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    mission_id      UUID NOT NULL REFERENCES public.missions(id) ON DELETE CASCADE,
    progress        INTEGER NOT NULL DEFAULT 0,
    completed       BOOLEAN NOT NULL DEFAULT FALSE,
    completed_at    TIMESTAMPTZ,
    period_start    DATE NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, mission_id, period_start)
);

-- =============================================================================
-- BADGES TABLE
-- =============================================================================
CREATE TABLE IF NOT EXISTS public.badges (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name_en     TEXT NOT NULL,
    name_ar     TEXT NOT NULL,
    icon_url    TEXT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================================
-- USER BADGES TABLE
-- =============================================================================
CREATE TABLE IF NOT EXISTS public.user_badges (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id     UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    badge_id    UUID NOT NULL REFERENCES public.badges(id) ON DELETE CASCADE,
    earned_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, badge_id)
);

-- =============================================================================
-- COSMETICS TABLE (card backs, etc.)
-- =============================================================================
CREATE TABLE IF NOT EXISTS public.cosmetics (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name_en     TEXT NOT NULL,
    name_ar     TEXT NOT NULL,
    type        TEXT NOT NULL,   -- 'card_back', 'table_skin', etc.
    asset_key   TEXT NOT NULL UNIQUE,
    coin_price  INTEGER NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS public.user_cosmetics (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id         UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    cosmetic_id     UUID NOT NULL REFERENCES public.cosmetics(id) ON DELETE CASCADE,
    purchased_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, cosmetic_id)
);

-- =============================================================================
-- LEADERBOARD VIEW (Classic mode, ELO-based)
-- =============================================================================
CREATE OR REPLACE VIEW public.leaderboard_classic AS
SELECT
    u.id,
    u.username,
    u.display_name,
    u.avatar_url,
    u.elo_rating,
    u.total_wins,
    u.total_losses,
    RANK() OVER (ORDER BY u.elo_rating DESC) AS rank
FROM public.users u
ORDER BY u.elo_rating DESC;

-- =============================================================================
-- ADMIN AUDIT: Track all modifications with user + source metadata
-- =============================================================================
ALTER ROLE postgres SET log_statement = 'mod';

-- =============================================================================
-- UPDATED_AT TRIGGER
-- =============================================================================
CREATE OR REPLACE FUNCTION public.set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_users_updated_at
    BEFORE UPDATE ON public.users
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

CREATE TRIGGER trg_matches_updated_at
    BEFORE UPDATE ON public.matches
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();

CREATE TRIGGER trg_game_state_updated_at
    BEFORE UPDATE ON public.game_state
    FOR EACH ROW EXECUTE FUNCTION public.set_updated_at();
