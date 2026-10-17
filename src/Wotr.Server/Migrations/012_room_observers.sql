-- OBS-01: up to sixteen observers per room. Membership and its lease live here, so heartbeats never lock the room row;
-- joins and role changes lock the room row first, which keeps the 2+16 limit exact.
CREATE TABLE room_observers (
    room_id text NOT NULL REFERENCES rooms(id) ON DELETE CASCADE,
    account_id text NOT NULL REFERENCES accounts(id),
    username text NOT NULL,
    joined_at timestamptz NOT NULL DEFAULT now(),
    lease_until timestamptz NOT NULL,
    PRIMARY KEY (room_id, account_id)
);
CREATE INDEX room_observers_account ON room_observers(account_id);

-- A host who moved to the observers may leave both player seats empty.
DO $$
DECLARE constraint_name text;
BEGIN
    SELECT conname INTO constraint_name FROM pg_constraint
    WHERE conrelid = 'rooms'::regclass AND contype = 'c'
        AND pg_get_constraintdef(oid) LIKE '%fp_account IS NOT NULL%OR%sh_account IS NOT NULL%';
    IF constraint_name IS NOT NULL THEN
        EXECUTE format('ALTER TABLE rooms DROP CONSTRAINT %I', constraint_name);
    END IF;
END $$;

-- The public list now shows started rooms too (for observing) and every open waiting room.
DROP INDEX IF EXISTS rooms_public_open;
CREATE INDEX rooms_public_listed ON rooms(id COLLATE "C") WHERE visibility='Public' AND NOT closed;
