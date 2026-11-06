-- RANK-06 (R35, docs/RANKED_PLAY.md D9): the running clock of each ranked match, changed in the transaction of the
-- command that moves the decision on, and the server's heartbeat, which measures downtime so that a restart can give
-- every running clock its lost time back.
CREATE TABLE match_clocks (
    match_id text PRIMARY KEY REFERENCES matches(id) ON DELETE CASCADE,
    decision_id text NOT NULL,
    owner text NOT NULL CHECK (owner IN ('FreePeoples', 'Shadow')),
    kind text NOT NULL CHECK (kind IN ('Big', 'Small')),
    limit_ms integer NOT NULL,
    deadline timestamptz NOT NULL
);
CREATE INDEX match_clocks_deadline ON match_clocks(deadline);

CREATE TABLE server_heartbeat (
    id integer PRIMARY KEY CHECK (id = 1),
    beat_at timestamptz NOT NULL
);
