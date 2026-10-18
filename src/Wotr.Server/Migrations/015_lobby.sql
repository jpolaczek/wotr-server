-- LOBBY-01: the Open games screen's own chat, who is on that screen now, and challenges between its players.
-- A message keeps the name its author had when writing it; the online list and challenges read current names.
CREATE TABLE lobby_messages (
    sequence bigserial PRIMARY KEY,
    account_id text NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    username text NOT NULL,
    text text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now()
);

-- One row per account: the last time its menu polled the lobby.
CREATE TABLE lobby_presence (
    account_id text PRIMARY KEY REFERENCES accounts(id) ON DELETE CASCADE,
    seen_at timestamptz NOT NULL
);
CREATE INDEX lobby_presence_seen ON lobby_presence(seen_at);

CREATE TABLE lobby_challenges (
    id text PRIMARY KEY,
    from_account text NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    to_account text NOT NULL REFERENCES accounts(id) ON DELETE CASCADE,
    status text NOT NULL CHECK (status IN ('Pending', 'Accepted', 'Declined', 'Cancelled', 'Expired')),
    created_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    room_id text,
    CHECK (from_account <> to_account)
);
CREATE INDEX lobby_challenges_to ON lobby_challenges(to_account, created_at DESC);
CREATE INDEX lobby_challenges_from ON lobby_challenges(from_account, created_at DESC);
