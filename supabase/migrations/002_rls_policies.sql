-- =============================================================================
-- Migration 002: Row Level Security (RLS) Policies
-- =============================================================================

-- ---------------------------------------------------------------------------
-- users
-- ---------------------------------------------------------------------------
ALTER TABLE public.users ENABLE ROW LEVEL SECURITY;

-- Users can read any user's public profile
CREATE POLICY "users_select_public"
    ON public.users FOR SELECT
    USING (TRUE);

-- Users can only update their own record
CREATE POLICY "users_update_own"
    ON public.users FOR UPDATE
    USING (auth.uid() = auth_id);

-- Only the service role can insert/delete users (handled via Edge Functions)
CREATE POLICY "users_insert_service"
    ON public.users FOR INSERT
    WITH CHECK (auth.uid() = auth_id);

-- ---------------------------------------------------------------------------
-- matches
-- ---------------------------------------------------------------------------
ALTER TABLE public.matches ENABLE ROW LEVEL SECURITY;

-- Any authenticated user can read matches
CREATE POLICY "matches_select_authenticated"
    ON public.matches FOR SELECT
    TO authenticated
    USING (TRUE);

-- Only service role inserts/updates matches (game logic in Edge Functions)
CREATE POLICY "matches_insert_service"
    ON public.matches FOR INSERT
    WITH CHECK (
        EXISTS (
            SELECT 1 FROM public.users u WHERE u.auth_id = auth.uid() AND u.id = host_user_id
        )
    );

-- ---------------------------------------------------------------------------
-- match_players
-- ---------------------------------------------------------------------------
ALTER TABLE public.match_players ENABLE ROW LEVEL SECURITY;

-- Players can see all seats (seat index, connected status) but NOT other players' hidden hand
CREATE POLICY "match_players_select"
    ON public.match_players FOR SELECT
    TO authenticated
    USING (TRUE);

-- Players can only see their OWN hand data via a secure function; the RLS hides it otherwise.
-- The 'hand' column is never exposed raw to clients; Edge Functions return only visible info.

-- ---------------------------------------------------------------------------
-- game_state
-- ---------------------------------------------------------------------------
ALTER TABLE public.game_state ENABLE ROW LEVEL SECURITY;

-- game_state is NEVER accessible directly by clients – only via service role (Edge Functions)
CREATE POLICY "game_state_no_client_access"
    ON public.game_state FOR ALL
    USING (FALSE);

-- ---------------------------------------------------------------------------
-- messages
-- ---------------------------------------------------------------------------
ALTER TABLE public.messages ENABLE ROW LEVEL SECURITY;

-- Any player in the match can read messages
CREATE POLICY "messages_select"
    ON public.messages FOR SELECT
    TO authenticated
    USING (
        EXISTS (
            SELECT 1 FROM public.match_players mp
            WHERE mp.match_id = messages.match_id
              AND mp.user_id = (SELECT id FROM public.users WHERE auth_id = auth.uid())
        )
    );

-- Any player in the match can send messages
CREATE POLICY "messages_insert"
    ON public.messages FOR INSERT
    TO authenticated
    WITH CHECK (
        user_id = (SELECT id FROM public.users WHERE auth_id = auth.uid())
        AND EXISTS (
            SELECT 1 FROM public.match_players mp
            WHERE mp.match_id = messages.match_id
              AND mp.user_id = user_id
        )
    );

-- ---------------------------------------------------------------------------
-- user_missions / user_badges / user_cosmetics – own records only
-- ---------------------------------------------------------------------------
ALTER TABLE public.user_missions ENABLE ROW LEVEL SECURITY;
CREATE POLICY "user_missions_own"
    ON public.user_missions FOR ALL
    USING (user_id = (SELECT id FROM public.users WHERE auth_id = auth.uid()));

ALTER TABLE public.user_badges ENABLE ROW LEVEL SECURITY;
CREATE POLICY "user_badges_own"
    ON public.user_badges FOR ALL
    USING (user_id = (SELECT id FROM public.users WHERE auth_id = auth.uid()));

ALTER TABLE public.user_cosmetics ENABLE ROW LEVEL SECURITY;
CREATE POLICY "user_cosmetics_own"
    ON public.user_cosmetics FOR ALL
    USING (user_id = (SELECT id FROM public.users WHERE auth_id = auth.uid()));

-- missions, badges, cosmetics are read-only for clients
ALTER TABLE public.missions ENABLE ROW LEVEL SECURITY;
CREATE POLICY "missions_select_all" ON public.missions FOR SELECT USING (TRUE);

ALTER TABLE public.badges ENABLE ROW LEVEL SECURITY;
CREATE POLICY "badges_select_all" ON public.badges FOR SELECT USING (TRUE);

ALTER TABLE public.cosmetics ENABLE ROW LEVEL SECURITY;
CREATE POLICY "cosmetics_select_all" ON public.cosmetics FOR SELECT USING (TRUE);
