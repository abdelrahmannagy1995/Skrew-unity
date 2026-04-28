-- =============================================================================
-- Migration 005: Helper RPCs for atomic increments
-- =============================================================================

-- Generic stat increment used by score-calc Edge Function
CREATE OR REPLACE FUNCTION public.increment_user_stat(
    p_user_id UUID,
    p_column  TEXT
)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
    IF p_column = 'total_wins' THEN
        UPDATE public.users SET total_wins   = total_wins   + 1 WHERE id = p_user_id;
    ELSIF p_column = 'total_losses' THEN
        UPDATE public.users SET total_losses = total_losses + 1 WHERE id = p_user_id;
    END IF;
END;
$$;

-- Award coins with idempotency guard (transaction_id prevents duplicate rewards)
CREATE TABLE IF NOT EXISTS public.coin_transactions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID NOT NULL REFERENCES public.users(id),
    transaction_id  TEXT NOT NULL UNIQUE,  -- AdMob transaction_id for SSV dedup
    coins           INTEGER NOT NULL,
    source          TEXT NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

ALTER TABLE public.coin_transactions ENABLE ROW LEVEL SECURITY;
CREATE POLICY "coin_transactions_no_client" ON public.coin_transactions FOR ALL USING (FALSE);

CREATE OR REPLACE FUNCTION public.award_coins(
    p_user_id        UUID,
    p_coins          INTEGER,
    p_transaction_id TEXT,
    p_source         TEXT
)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
BEGIN
    -- Insert transaction record; will fail on duplicate (idempotent)
    INSERT INTO public.coin_transactions (user_id, transaction_id, coins, source)
    VALUES (p_user_id, p_transaction_id, p_coins, p_source)
    ON CONFLICT (transaction_id) DO NOTHING;

    -- Only award coins if the transaction was newly inserted
    IF FOUND THEN
        UPDATE public.users SET coins = coins + p_coins WHERE id = p_user_id;
    END IF;
END;
$$;

-- Mission progress increment (used by basra-resolve, thief-guess Edge Functions)
CREATE OR REPLACE FUNCTION public.increment_mission_progress(
    p_user_id    UUID,
    p_mission_key TEXT
)
RETURNS VOID
LANGUAGE plpgsql
SECURITY DEFINER
AS $$
DECLARE
    v_mission_id UUID;
    v_target     INTEGER;
    v_coin_reward INTEGER;
    v_badge_id   UUID;
    v_today      DATE := CURRENT_DATE;
BEGIN
    -- Find the mission by key (match English title pattern or a key column if added)
    SELECT id, target_count, coin_reward, badge_id
    INTO v_mission_id, v_target, v_coin_reward, v_badge_id
    FROM public.missions
    WHERE LOWER(title_en) LIKE '%' || LOWER(p_mission_key) || '%'
    LIMIT 1;

    IF v_mission_id IS NULL THEN RETURN; END IF;

    -- Upsert user_mission progress
    INSERT INTO public.user_missions (user_id, mission_id, progress, period_start)
    VALUES (p_user_id, v_mission_id, 1, v_today)
    ON CONFLICT (user_id, mission_id, period_start)
    DO UPDATE SET progress = user_missions.progress + 1;

    -- Check for completion
    UPDATE public.user_missions
    SET completed = TRUE, completed_at = NOW()
    WHERE user_id = p_user_id
      AND mission_id = v_mission_id
      AND period_start = v_today
      AND progress >= v_target
      AND completed = FALSE;

    -- Award coins and badge if completed
    IF FOUND THEN
        UPDATE public.users SET coins = coins + v_coin_reward WHERE id = p_user_id;

        IF v_badge_id IS NOT NULL THEN
            INSERT INTO public.user_badges (user_id, badge_id)
            VALUES (p_user_id, v_badge_id)
            ON CONFLICT DO NOTHING;
        END IF;
    END IF;
END;
$$;
