-- ADMIN-01: what the admin panel counts. Accounts and sessions remember when they were made, a session says whether it
-- is a game or an admin session, and two ledgers keep every table and every match that ever started, also after its
-- players remove it, with the match's kind (a game, a test scenario, a demo against the CPU) and result.
ALTER TABLE accounts ADD COLUMN created_at timestamptz;
ALTER TABLE accounts ALTER COLUMN created_at SET DEFAULT now();
ALTER TABLE sessions ADD COLUMN created_at timestamptz;
ALTER TABLE sessions ALTER COLUMN created_at SET DEFAULT now();
ALTER TABLE sessions ADD COLUMN purpose text NOT NULL DEFAULT 'game' CHECK (purpose IN ('game', 'admin'));

-- Every session before this migration was issued for 30 days (SessionStore), and registration issues an account's
-- first session, so both times are known exactly for the rows already there. Those were 720 hours: '30 days' would
-- follow the calendar of the session's time zone and shift by an hour across a daylight-saving change.
UPDATE sessions SET created_at = expires_at - interval '720 hours';
UPDATE accounts a SET created_at = first.created_at
FROM (SELECT account_id, min(created_at) AS created_at FROM sessions GROUP BY account_id) first
WHERE first.account_id = a.id;
CREATE INDEX sessions_account_created ON sessions(account_id, created_at DESC);

CREATE TABLE match_ledger (
    match_id text PRIMARY KEY,
    kind text NOT NULL CHECK (kind IN ('game', 'test', 'demo')),
    fp_account text NOT NULL,
    sh_account text NOT NULL,
    created_at timestamptz,
    finished_at timestamptz,
    winner text
);
ALTER TABLE match_ledger ALTER COLUMN created_at SET DEFAULT now();
INSERT INTO match_ledger(match_id, kind, fp_account, sh_account, created_at, finished_at, winner)
SELECT m.id,
       CASE WHEN t.match_id IS NOT NULL THEN 'test' WHEN d.match_id IS NOT NULL THEN 'demo' ELSE 'game' END,
       m.fp_account,
       m.sh_account,
       COALESCE(t.created_at, d.created_at, (SELECT min(f.recorded_at) FROM match_frames f WHERE f.match_id = m.id)),
       CASE WHEN m.state->>'winner' IS NOT NULL THEN m.updated_at END,
       m.state->>'winner'
FROM matches m
LEFT JOIN test_matches t ON t.match_id = m.id
LEFT JOIN demo_matches d ON d.match_id = m.id;
CREATE INDEX match_ledger_fp ON match_ledger(fp_account) WHERE kind = 'game';
CREATE INDEX match_ledger_sh ON match_ledger(sh_account) WHERE kind = 'game';

CREATE TABLE room_ledger (
    room_id text PRIMARY KEY,
    host_account text NOT NULL,
    created_at timestamptz
);
ALTER TABLE room_ledger ALTER COLUMN created_at SET DEFAULT now();
-- A table's creation time was never stored; the rooms already there count without one.
INSERT INTO room_ledger(room_id, host_account, created_at)
SELECT id, host_account, NULL FROM rooms WHERE host_account IS NOT NULL;
CREATE INDEX room_ledger_host ON room_ledger(host_account);
