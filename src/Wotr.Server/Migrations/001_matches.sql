CREATE TABLE IF NOT EXISTS schema_migrations (version integer PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now());
CREATE TABLE IF NOT EXISTS matches (
    id text PRIMARY KEY,
    fp_account text NOT NULL,
    sh_account text NOT NULL,
    revision bigint NOT NULL,
    state jsonb NOT NULL,
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (fp_account <> sh_account)
);
CREATE INDEX IF NOT EXISTS matches_fp ON matches(fp_account);
CREATE INDEX IF NOT EXISTS matches_sh ON matches(sh_account);
CREATE TABLE IF NOT EXISTS command_receipts (
    match_id text NOT NULL REFERENCES matches(id),
    account_id text NOT NULL,
    command_id text NOT NULL,
    fingerprint text NOT NULL,
    receipt jsonb NOT NULL,
    PRIMARY KEY(match_id,account_id,command_id)
);
CREATE TABLE IF NOT EXISTS match_events (
    match_id text NOT NULL REFERENCES matches(id),
    revision bigint NOT NULL,
    ordinal integer NOT NULL,
    event jsonb NOT NULL,
    PRIMARY KEY(match_id,revision,ordinal)
);
