-- REPLAY-01: a recorded match keeps one frame per public change: its first state and the state after every accepted
-- decision, as both sides see it (read-only views, no options or tokens). History is not copied into frames; it
-- stays in match_events with the revision it belongs to. Matches created before this migration are not recorded.
ALTER TABLE matches ADD COLUMN recorded boolean NOT NULL DEFAULT false;
CREATE TABLE match_frames (
    match_id text NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    step integer NOT NULL,
    revision bigint NOT NULL,
    turn integer NOT NULL,
    phase text NOT NULL,
    free_peoples jsonb NOT NULL,
    shadow jsonb NOT NULL,
    recorded_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (match_id, step),
    UNIQUE (match_id, revision)
);
