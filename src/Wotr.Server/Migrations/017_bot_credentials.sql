-- BOT-16: authority only. Policy memory, command journals and game snapshots never belong here.
CREATE TABLE bot_seat_grants (
    id text PRIMARY KEY,
    service_id text NOT NULL,
    participant_id text NOT NULL UNIQUE REFERENCES accounts(id),
    room_id text REFERENCES rooms(id) ON DELETE CASCADE,
    match_id text REFERENCES matches(id) ON DELETE CASCADE,
    side text NOT NULL CHECK (side IN ('FreePeoples', 'Shadow')),
    generation bigint NOT NULL CHECK (generation > 0),
    lease_until timestamptz NOT NULL,
    revoked boolean NOT NULL DEFAULT false,
    CHECK (room_id IS NOT NULL OR match_id IS NOT NULL)
);
CREATE TABLE bot_seat_credentials (
    token_hash text PRIMARY KEY,
    grant_id text NOT NULL REFERENCES bot_seat_grants(id) ON DELETE CASCADE,
    generation bigint NOT NULL,
    expires_at timestamptz NOT NULL,
    revoked boolean NOT NULL DEFAULT false
);
CREATE INDEX bot_credentials_grant ON bot_seat_credentials(grant_id);
