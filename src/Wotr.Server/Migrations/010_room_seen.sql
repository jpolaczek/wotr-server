ALTER TABLE rooms ADD COLUMN seen_at timestamptz NOT NULL DEFAULT now();
CREATE INDEX rooms_unstarted_seen ON rooms(seen_at) WHERE match_id IS NULL;
