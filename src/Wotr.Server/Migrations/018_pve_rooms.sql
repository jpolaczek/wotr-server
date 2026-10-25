-- Identity and lifecycle only; private policy state and journals belong to the bot service.
CREATE TABLE bot_assignments (
    id text PRIMARY KEY,
    room_id text NOT NULL REFERENCES rooms(id) ON DELETE CASCADE,
    host_account text NOT NULL REFERENCES accounts(id),
    participant_id text NOT NULL UNIQUE REFERENCES accounts(id),
    request_id text NOT NULL,
    requested_version integer NOT NULL,
    room_version integer NOT NULL,
    service_id text NOT NULL,
    side text NOT NULL CHECK (side IN ('FreePeoples','Shadow')),
    profile_id text NOT NULL,
    policy_version text NOT NULL,
    status text NOT NULL CHECK (status IN ('pending','started','cancelled')),
    match_id text UNIQUE REFERENCES matches(id) ON DELETE CASCADE,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (room_id,host_account,request_id)
);
CREATE UNIQUE INDEX bot_assignment_live_room ON bot_assignments(room_id) WHERE status <> 'cancelled';
ALTER TABLE match_ledger DROP CONSTRAINT match_ledger_kind_check;
ALTER TABLE match_ledger ADD CONSTRAINT match_ledger_kind_check CHECK (kind IN ('game','test','demo','pve'));
