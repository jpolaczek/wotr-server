CREATE TABLE demo_matches (
    match_id text PRIMARY KEY REFERENCES matches(id) ON DELETE CASCADE,
    owner_account text NOT NULL REFERENCES accounts(id),
    cpu_side text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);
CREATE UNIQUE INDEX demo_matches_owner ON demo_matches(owner_account);
