-- QUAL-03: accepted receipts stay; domain rejections are bounded. Each receipt says whether it was accepted, at
-- which match revision it was answered and when it was stored, so rejections answered before the match moved on
-- can be removed (a retry is then validated afresh at the new revision and cannot run its old intent).
ALTER TABLE command_receipts ADD COLUMN IF NOT EXISTS accepted boolean;
ALTER TABLE command_receipts ADD COLUMN IF NOT EXISTS revision bigint;
ALTER TABLE command_receipts ADD COLUMN IF NOT EXISTS stored_at timestamptz;
UPDATE command_receipts
SET accepted = COALESCE((receipt->>'accepted')::boolean, false),
    revision = COALESCE((receipt->>'revision')::bigint, 0),
    stored_at = now()
WHERE accepted IS NULL OR revision IS NULL OR stored_at IS NULL;
ALTER TABLE command_receipts ALTER COLUMN accepted SET NOT NULL;
ALTER TABLE command_receipts ALTER COLUMN revision SET NOT NULL;
ALTER TABLE command_receipts ALTER COLUMN stored_at SET NOT NULL;
ALTER TABLE command_receipts ALTER COLUMN stored_at SET DEFAULT clock_timestamp();
-- A writer unaware of the columns stores a row that counts as accepted, so it is never removed.
ALTER TABLE command_receipts ALTER COLUMN accepted SET DEFAULT true;
ALTER TABLE command_receipts ALTER COLUMN revision SET DEFAULT 0;
CREATE INDEX IF NOT EXISTS command_receipts_rejections ON command_receipts(match_id, account_id, revision) WHERE NOT accepted;
-- One-time removal of rejections that older revisions already outdated, in portions of 5000 rows.
DO $$
DECLARE removed integer;
BEGIN
    LOOP
        DELETE FROM command_receipts
        WHERE ctid IN (
            SELECT r.ctid FROM command_receipts r JOIN matches m ON m.id = r.match_id
            WHERE NOT r.accepted AND r.revision < m.revision
            LIMIT 5000);
        GET DIAGNOSTICS removed = ROW_COUNT;
        EXIT WHEN removed = 0;
    END LOOP;
END $$;
