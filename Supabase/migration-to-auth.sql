-- ============================================================
-- Migration: device_uuid → user_id + Supabase Auth
-- Run this ONCE in both dev and prod SQL Editor
-- WARNING: Drops all existing data (no real users yet)
-- ============================================================

-- Drop existing objects
DROP FUNCTION IF EXISTS get_player_rank(TEXT, DATE);
DROP TABLE IF EXISTS daily_scores;
DROP TABLE IF EXISTS players;

-- Recreate with user_id
CREATE TABLE players (
    user_id TEXT PRIMARY KEY,
    display_name TEXT NOT NULL DEFAULT 'Player',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    current_streak INTEGER NOT NULL DEFAULT 0,
    longest_streak INTEGER NOT NULL DEFAULT 0,
    name_changes_today INTEGER NOT NULL DEFAULT 0,
    name_change_date DATE,
    auth_provider TEXT DEFAULT 'anonymous',
    auth_email TEXT
);

CREATE TABLE daily_scores (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id TEXT NOT NULL REFERENCES players(user_id),
    display_name TEXT DEFAULT 'Player',
    game_date DATE NOT NULL,
    score INTEGER NOT NULL,
    day_number INTEGER NOT NULL DEFAULT 1,
    merge_counts INTEGER[] DEFAULT '{}',
    longest_chain INTEGER NOT NULL DEFAULT 0,
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (user_id, game_date)
);

-- Indexes
CREATE INDEX idx_daily_scores_date_score ON daily_scores (game_date, score DESC);
CREATE INDEX idx_daily_scores_user_date ON daily_scores (user_id, game_date);

-- Rank function
CREATE OR REPLACE FUNCTION get_player_rank(p_user_id TEXT, p_game_date DATE)
RETURNS TABLE(rank BIGINT, total_players BIGINT) AS $$
  SELECT r.rank, r.total_players
  FROM (
    SELECT user_id,
           RANK() OVER (ORDER BY score DESC) AS rank,
           COUNT(*) OVER () AS total_players
    FROM daily_scores
    WHERE game_date = p_game_date
  ) r
  WHERE r.user_id = p_user_id;
$$ LANGUAGE sql STABLE;

-- RLS
ALTER TABLE players ENABLE ROW LEVEL SECURITY;
ALTER TABLE daily_scores ENABLE ROW LEVEL SECURITY;

CREATE POLICY "Players are viewable by everyone" ON players FOR SELECT USING (true);
CREATE POLICY "Users can manage own player" ON players FOR ALL
    USING (auth.uid()::text = user_id) WITH CHECK (auth.uid()::text = user_id);

CREATE POLICY "Scores are viewable by everyone" ON daily_scores FOR SELECT USING (true);
CREATE POLICY "Users can insert own scores" ON daily_scores FOR INSERT
    WITH CHECK (auth.uid()::text = user_id);

-- Allow service role full access (edge functions bypass RLS anyway)
