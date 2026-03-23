-- ============================================================
-- Supabase Schema for Overtone
-- Run this in the Supabase SQL Editor to set up tables and RLS
-- ============================================================

-- Players table
-- device_uuid is the identity key. Future account system can add a parent table
-- that links one or more device_uuids.
CREATE TABLE IF NOT EXISTS players (
    device_uuid TEXT PRIMARY KEY,
    display_name TEXT NOT NULL DEFAULT 'Player',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    current_streak INTEGER NOT NULL DEFAULT 0,
    longest_streak INTEGER NOT NULL DEFAULT 0
);

-- Daily scores table
-- One scored attempt per player per day
CREATE TABLE IF NOT EXISTS daily_scores (
    id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    device_uuid TEXT NOT NULL REFERENCES players(device_uuid),
    game_date DATE NOT NULL,
    score INTEGER NOT NULL,
    day_number INTEGER NOT NULL DEFAULT 1,
    merge_counts INTEGER[] DEFAULT '{}',
    submitted_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE (device_uuid, game_date)
);

-- Indexes for leaderboard queries
CREATE INDEX IF NOT EXISTS idx_daily_scores_date_score
    ON daily_scores (game_date, score DESC);

CREATE INDEX IF NOT EXISTS idx_daily_scores_uuid_date
    ON daily_scores (device_uuid, game_date);

-- ============================================================
-- Row Level Security
-- ============================================================

-- Enable RLS on both tables
ALTER TABLE players ENABLE ROW LEVEL SECURITY;
ALTER TABLE daily_scores ENABLE ROW LEVEL SECURITY;

-- Players: anyone can read (for leaderboard display names)
-- Insert/update only via Edge Functions (service role)
CREATE POLICY "Players are viewable by everyone"
    ON players FOR SELECT
    USING (true);

-- Daily scores: anyone can read (leaderboard)
-- Insert only via Edge Functions (service role)
CREATE POLICY "Scores are viewable by everyone"
    ON daily_scores FOR SELECT
    USING (true);

-- Edge Functions use the service_role key, which bypasses RLS.
-- The anon key (used by the game client) can only SELECT.
-- This means all writes go through Edge Functions, which validate data.
