-- RANK-09 (R35, docs/RANKED_PLAY.md D14): a test scenario may carry a clock with short limits, for playtesting alone. It has
-- no ranked seats, which are what a rating follows, so it can never change one.
ALTER TABLE ranked_matches DROP CONSTRAINT ranked_matches_preset_check;
ALTER TABLE ranked_matches ADD CONSTRAINT ranked_matches_preset_check CHECK (preset IN ('Fast', 'Standard', 'Long', 'Rehearsal'));
