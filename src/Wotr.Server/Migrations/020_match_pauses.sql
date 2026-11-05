-- RANK-07 (R35, docs/RANKED_PLAY.md): a paused clock and the pauses each player has spent. A pause is no rules event: it
-- changes no game state and no replay, so its own version tells the clients about it without a revision.
ALTER TABLE match_clocks ADD COLUMN paused_at timestamptz;
ALTER TABLE match_clocks ADD COLUMN paused_by text CHECK (paused_by IN ('FreePeoples', 'Shadow'));
ALTER TABLE match_clocks ADD COLUMN version bigint NOT NULL DEFAULT 0;
ALTER TABLE ranked_matches ADD COLUMN fp_pauses integer NOT NULL DEFAULT 0;
ALTER TABLE ranked_matches ADD COLUMN sh_pauses integer NOT NULL DEFAULT 0;
