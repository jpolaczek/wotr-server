-- No global snapshot scan at startup. A legacy match is indexed on its next accepted write.
ALTER TABLE matches ADD COLUMN events_indexed boolean NOT NULL DEFAULT false;
ALTER TABLE match_events ADD COLUMN position bigint;
ALTER TABLE match_events ADD COLUMN event_id text;
ALTER TABLE match_events ADD COLUMN action_id text;
CREATE UNIQUE INDEX match_event_position ON match_events(match_id,position) WHERE position IS NOT NULL;
CREATE UNIQUE INDEX match_event_identity ON match_events(match_id,event_id) WHERE event_id IS NOT NULL;
CREATE INDEX match_event_action ON match_events(match_id,action_id,position) WHERE position IS NOT NULL;
