-- ============================================================
-- Supabase Schema for Murge
-- Run this in the Supabase SQL Editor to set up tables and RLS
-- ============================================================

-- Players table
-- user_id is the Supabase auth user ID (from anonymous or linked auth)
CREATE TABLE IF NOT EXISTS players (
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

-- Daily scores table
-- One scored attempt per player per day
CREATE TABLE IF NOT EXISTS daily_scores (
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

-- Indexes for leaderboard queries
CREATE INDEX IF NOT EXISTS idx_daily_scores_date_score
    ON daily_scores (game_date, score DESC);

CREATE INDEX IF NOT EXISTS idx_daily_scores_user_date
    ON daily_scores (user_id, game_date);

-- ============================================================
-- Functions
-- ============================================================

-- Single-query rank lookup using window functions
-- Called by get-player-rank edge function via supabase.rpc()
CREATE OR REPLACE FUNCTION get_player_rank(p_user_id TEXT, p_game_date DATE)
RETURNS TABLE(rank BIGINT, total_players BIGINT) AS $$
  SELECT
    r.rank,
    r.total_players
  FROM (
    SELECT
      user_id,
      RANK() OVER (ORDER BY score DESC) AS rank,
      COUNT(*) OVER () AS total_players
    FROM daily_scores
    WHERE game_date = p_game_date
  ) r
  WHERE r.user_id = p_user_id;
$$ LANGUAGE sql STABLE;

-- ============================================================
-- Row Level Security
-- ============================================================

-- Enable RLS on both tables
ALTER TABLE players ENABLE ROW LEVEL SECURITY;
ALTER TABLE daily_scores ENABLE ROW LEVEL SECURITY;

-- Players: anyone can read (for leaderboard display names)
CREATE POLICY "Players are viewable by everyone"
    ON players FOR SELECT
    USING (true);

-- Players: authenticated users can insert/update their own row
CREATE POLICY "Users can manage own player"
    ON players FOR ALL
    USING (auth.uid()::text = user_id)
    WITH CHECK (auth.uid()::text = user_id);

-- Daily scores: anyone can read (leaderboard)
CREATE POLICY "Scores are viewable by everyone"
    ON daily_scores FOR SELECT
    USING (true);

-- Daily scores: authenticated users can insert their own scores
CREATE POLICY "Users can insert own scores"
    ON daily_scores FOR INSERT
    WITH CHECK (auth.uid()::text = user_id);

-- Edge Functions still use service_role key which bypasses RLS.
-- The anon key can SELECT. Authenticated users can write their own data.
-- TODO: Once JWT auth is fully rolled out, remove API key auth from edge functions.
