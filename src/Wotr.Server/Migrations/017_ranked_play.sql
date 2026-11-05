-- RANK-04 (R35, docs/RANKED_PLAY.md): ranked matches, each account's single rating and every change of it.

-- The ledger counts ranked matches by their own kind.
DO $$
DECLARE constraint_name text;
BEGIN
    SELECT conname INTO constraint_name FROM pg_constraint
    WHERE conrelid = 'match_ledger'::regclass AND contype = 'c' AND pg_get_constraintdef(oid) LIKE '%kind%';
    IF constraint_name IS NOT NULL THEN
        EXECUTE format('ALTER TABLE match_ledger DROP CONSTRAINT %I', constraint_name);
    END IF;
END $$;
ALTER TABLE match_ledger ADD CONSTRAINT match_ledger_kind CHECK (kind IN ('game', 'test', 'demo', 'ranked'));
-- Games between people include ranked ones; the per-player counts read both kinds.
DROP INDEX match_ledger_fp;
DROP INDEX match_ledger_sh;
CREATE INDEX match_ledger_fp ON match_ledger(fp_account) WHERE kind IN ('game', 'ranked');
CREATE INDEX match_ledger_sh ON match_ledger(sh_account) WHERE kind IN ('game', 'ranked');

CREATE TABLE ranked_matches (
    match_id text PRIMARY KEY REFERENCES matches(id) ON DELETE CASCADE,
    preset text NOT NULL CHECK (preset IN ('Fast', 'Standard', 'Long')),
    created_at timestamptz NOT NULL DEFAULT now()
);

-- An account's active ranked match. The row goes in the transaction that finishes the match, so an account holds at
-- most one, and its removal is what triggers the rating update exactly once.
CREATE TABLE ranked_seats (
    account_id text PRIMARY KEY REFERENCES accounts(id),
    match_id text NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    side text NOT NULL CHECK (side IN ('FreePeoples', 'Shadow'))
);
CREATE INDEX ranked_seats_match ON ranked_seats(match_id);

CREATE TABLE player_ratings (
    account_id text PRIMARY KEY REFERENCES accounts(id),
    rating double precision NOT NULL,
    deviation double precision NOT NULL,
    volatility double precision NOT NULL,
    games integer NOT NULL DEFAULT 0,
    wins integer NOT NULL DEFAULT 0,
    losses integer NOT NULL DEFAULT 0,
    updated_at timestamptz NOT NULL DEFAULT now()
);
-- The leaderboard's order (D13): rating, then more games, then account id.
CREATE INDEX player_ratings_order ON player_ratings(rating DESC, games DESC, account_id) WHERE games > 0;

CREATE TABLE rating_changes (
    match_id text NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    account_id text NOT NULL REFERENCES accounts(id),
    rating_before double precision NOT NULL,
    rating_after double precision NOT NULL,
    deviation_before double precision NOT NULL,
    deviation_after double precision NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (match_id, account_id)
);
