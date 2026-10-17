CREATE TABLE accounts (
    id text PRIMARY KEY,
    username text NOT NULL,
    normalized_name text NOT NULL UNIQUE,
    password_hash text NOT NULL,
    is_admin boolean NOT NULL DEFAULT false
);
CREATE TABLE invitations (
    token_hash text PRIMARY KEY,
    expires_at timestamptz NOT NULL,
    grants_admin boolean NOT NULL DEFAULT false,
    consumed_by text REFERENCES accounts(id)
);
CREATE TABLE sessions (
    token_hash text PRIMARY KEY,
    account_id text NOT NULL REFERENCES accounts(id),
    expires_at timestamptz NOT NULL,
    revoked boolean NOT NULL DEFAULT false
);
CREATE INDEX sessions_account ON sessions(account_id);
CREATE TABLE rooms (
    id text PRIMARY KEY,
    join_code text NOT NULL UNIQUE,
    fp_account text REFERENCES accounts(id),
    sh_account text REFERENCES accounts(id),
    state jsonb NOT NULL,
    CHECK(fp_account IS NOT NULL OR sh_account IS NOT NULL),
    CHECK(fp_account IS NULL OR sh_account IS NULL OR fp_account <> sh_account)
);
CREATE INDEX rooms_fp ON rooms(fp_account);
CREATE INDEX rooms_sh ON rooms(sh_account);
