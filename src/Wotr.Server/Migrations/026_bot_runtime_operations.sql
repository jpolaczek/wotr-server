CREATE TABLE bot_runtime_control (
    id integer PRIMARY KEY CHECK (id=1),
    epoch bigint NOT NULL DEFAULT 0,
    draining boolean NOT NULL DEFAULT false,
    runtime_version text,
    manifest_sha256 text,
    updated_at timestamptz NOT NULL DEFAULT now()
);
INSERT INTO bot_runtime_control(id) VALUES (1);

-- NULL deliberately means unreviewed legacy metadata, never implicit compatibility.
ALTER TABLE bot_runtime_jobs ADD COLUMN compatibility jsonb;
