-- =============================================================================
-- Migration 003: pg_cron Turn Timer Jobs
-- =============================================================================

-- Schedule a turn-expiry check every 5 seconds using pg_cron.
-- The Edge Function 'game-tick' handles the actual expiry logic;
-- this cron job acts as the heartbeat trigger.
SELECT cron.schedule(
    'expire-turn-timers',
    '5 seconds',
    $$
    SELECT net.http_post(
        url := current_setting('app.supabase_functions_url') || '/game-tick',
        headers := jsonb_build_object(
            'Content-Type', 'application/json',
            'Authorization', 'Bearer ' || current_setting('app.service_role_key')
        ),
        body := '{}'::jsonb
    ) AS request_id;
    $$
);

-- Daily streak reset at midnight UTC
SELECT cron.schedule(
    'reset-daily-missions',
    '0 0 * * *',
    $$
    -- Mark uncompleted daily missions as expired (new ones are created on login)
    UPDATE public.user_missions
    SET completed = FALSE, progress = 0
    WHERE period_start < CURRENT_DATE
      AND completed = FALSE
      AND EXISTS (
          SELECT 1 FROM public.missions m
          WHERE m.id = user_missions.mission_id AND m.period = 'daily'
      );
    $$
);

-- Weekly mission reset every Monday at midnight UTC
SELECT cron.schedule(
    'reset-weekly-missions',
    '0 0 * * 1',
    $$
    UPDATE public.user_missions
    SET completed = FALSE, progress = 0
    WHERE period_start < DATE_TRUNC('week', CURRENT_DATE)
      AND completed = FALSE
      AND EXISTS (
          SELECT 1 FROM public.missions m
          WHERE m.id = user_missions.mission_id AND m.period = 'weekly'
      );
    $$
);
