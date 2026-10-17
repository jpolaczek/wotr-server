-- AUTH-02 (D16): an email address on each new account, confirmation and password reset links. Accounts created
-- before AUTH-02 may have no address. invitations stays unused, so an older server image can still run.
ALTER TABLE accounts ADD COLUMN email text;
ALTER TABLE accounts ADD COLUMN normalized_email text;
ALTER TABLE accounts ADD COLUMN email_verified_at timestamptz;
CREATE UNIQUE INDEX accounts_normalized_email ON accounts(normalized_email) WHERE normalized_email IS NOT NULL;

CREATE TABLE account_tokens (
    token_hash text PRIMARY KEY,
    account_id text NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    purpose text NOT NULL CHECK (purpose IN ('verify-email', 'reset-password')),
    email text NOT NULL,
    created_at timestamptz NOT NULL,
    expires_at timestamptz NOT NULL,
    consumed_at timestamptz
);
CREATE INDEX account_tokens_account ON account_tokens(account_id, created_at);
CREATE INDEX account_tokens_created ON account_tokens(created_at);
