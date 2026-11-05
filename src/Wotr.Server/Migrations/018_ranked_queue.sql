-- RANK-05 (R35, docs/RANKED_PLAY.md D5): one live search per account, held in the database so every server process
-- pairs the same queue. A search stays eligible only while its client polls; times come from the server's clock.
CREATE TABLE ranked_tickets (
    account_id text PRIMARY KEY REFERENCES accounts(id),
    search jsonb NOT NULL,
    client_version text NOT NULL DEFAULT '',
    created_at timestamptz NOT NULL,
    polled_at timestamptz NOT NULL
);
CREATE INDEX ranked_tickets_polled ON ranked_tickets(polled_at);
