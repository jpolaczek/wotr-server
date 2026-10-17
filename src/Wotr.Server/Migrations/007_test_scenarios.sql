ALTER TABLE accounts ADD COLUMN technical boolean NOT NULL DEFAULT false;
CREATE TABLE test_matches (
    match_id text PRIMARY KEY REFERENCES matches(id) ON DELETE CASCADE,
    owner_account text NOT NULL REFERENCES accounts(id),
    scenario_id text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX test_matches_owner ON test_matches(owner_account);
