-- Only existing event rows, never a global expansion of historical match snapshots.
CREATE INDEX match_event_battle ON match_events
    (match_id, (event->'activity'->'context'->>'battleId'), position)
    WHERE position IS NOT NULL AND event->>'onlyFor' IS NULL;
