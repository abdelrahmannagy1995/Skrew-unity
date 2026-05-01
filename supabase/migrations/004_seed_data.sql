-- =============================================================================
-- Migration 004: Seed Data – Missions, Badges, Cosmetics
-- =============================================================================

-- Badges
INSERT INTO public.badges (id, name_en, name_ar, icon_url) VALUES
    ('11111111-0000-0000-0000-000000000001', 'Basra Master',     'سيد البصرة',       'badges/basra_master.png'),
    ('11111111-0000-0000-0000-000000000002', 'Thief Catcher',    'صائد الحرامي',     'badges/thief_catcher.png'),
    ('11111111-0000-0000-0000-000000000003', 'Screw Champion',   'بطل السكرو',        'badges/screw_champion.png'),
    ('11111111-0000-0000-0000-000000000004', 'Win Streak x5',    'سلسلة انتصارات x5', 'badges/streak_5.png'),
    ('11111111-0000-0000-0000-000000000005', 'Perfect Screw',    'سكرو مثالي',        'badges/perfect_screw.png')
ON CONFLICT DO NOTHING;

-- Daily Missions
INSERT INTO public.missions (title_en, title_ar, description_en, description_ar, period, target_count, coin_reward, badge_id) VALUES
    ('Basra Blitz',     'بصرة السرعة',   'Execute 5 successful out-of-turn Basra matches today',   'نجح في 5 مطابقات بصرة خارج الدور اليوم',   'daily', 5,  100, '11111111-0000-0000-0000-000000000001'),
    ('Thief Hunter',    'صائد الحرامي',  'Catch the Thief 1 time today',                           'امسك الحرامي مرة واحدة اليوم',             'daily', 1,  150, '11111111-0000-0000-0000-000000000002'),
    ('Daily Screw',     'سكرو اليومي',   'Win 3 rounds of any game mode today',                    'فز بـ 3 جولات من أي وضع لعبة اليوم',       'daily', 3,  75,  NULL),
    ('Lone Wolf',       'الذئب المنفرد', 'Call Screw and win without penalty today',                'اتصل بالسكرو وفز بدون عقوبة اليوم',        'daily', 1,  200, '11111111-0000-0000-0000-000000000005')
ON CONFLICT DO NOTHING;

-- Weekly Missions
INSERT INTO public.missions (title_en, title_ar, description_en, description_ar, period, target_count, coin_reward, badge_id) VALUES
    ('Basra Veteran',   'مخضرم البصرة',  'Execute 25 successful Basra matches this week',          'نجح في 25 مطابقة بصرة هذا الأسبوع',        'weekly', 25, 500, '11111111-0000-0000-0000-000000000001'),
    ('Thief Expert',    'خبير الحرامي',  'Catch the Thief 3 times this week',                      'امسك الحرامي 3 مرات هذا الأسبوع',          'weekly', 3,  600, '11111111-0000-0000-0000-000000000002'),
    ('Winning Streak',  'سلسلة الفوز',   'Win 5 consecutive rounds this week',                     'فز بـ 5 جولات متتالية هذا الأسبوع',        'weekly', 5,  750, '11111111-0000-0000-0000-000000000004')
ON CONFLICT DO NOTHING;

-- Cosmetics (card backs)
INSERT INTO public.cosmetics (name_en, name_ar, type, asset_key, coin_price) VALUES
    ('Classic Blue',    'الكلاسيكي الأزرق',    'card_back', 'card_back_classic_blue',   0),
    ('Desert Gold',     'ذهب الصحراء',         'card_back', 'card_back_desert_gold',    500),
    ('Neon Nights',     'ليالي النيون',         'card_back', 'card_back_neon_nights',    750),
    ('Hieroglyphs',     'الهيروغليفية',         'card_back', 'card_back_hieroglyphs',    1000),
    ('Royal Red',       'الأحمر الملكي',        'card_back', 'card_back_royal_red',      1200)
ON CONFLICT DO NOTHING;
