ALTER TABLE bot_assignments ADD COLUMN runtime_version text;

CREATE TABLE bot_runtime_jobs (
    assignment_id text PRIMARY KEY REFERENCES bot_assignments(id) ON DELETE CASCADE,
    generation bigint NOT NULL DEFAULT 0,
    owner_id text NOT NULL DEFAULT '',
    lease_until timestamptz NOT NULL DEFAULT '-infinity',
    available_at timestamptz NOT NULL DEFAULT now(),
    health_until timestamptz NOT NULL DEFAULT '-infinity',
    ready_version integer,
    memory text,
    memory_version bigint NOT NULL DEFAULT 0,
    history_position bigint NOT NULL DEFAULT 0,
    accepted_revision bigint,
    last_error text NOT NULL DEFAULT ''
);
CREATE INDEX bot_runtime_jobs_due ON bot_runtime_jobs(available_at, lease_until);

CREATE TABLE bot_runtime_proposals (
    assignment_id text PRIMARY KEY REFERENCES bot_runtime_jobs(assignment_id) ON DELETE CASCADE,
    invocation_id text NOT NULL UNIQUE,
    memory_version bigint NOT NULL,
    history_position bigint NOT NULL,
    command jsonb NOT NULL,
    fingerprint text NOT NULL,
    proposed_memory text,
    status text NOT NULL CHECK (status IN ('pending', 'accepted', 'rejected')),
    result_revision bigint,
    error text NOT NULL DEFAULT ''
);
