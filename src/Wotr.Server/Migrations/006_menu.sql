ALTER TABLE rooms ADD COLUMN room_name text NOT NULL DEFAULT '';
ALTER TABLE rooms ADD COLUMN host_account text REFERENCES accounts(id);
ALTER TABLE rooms ADD COLUMN visibility text NOT NULL DEFAULT 'Private' CHECK (visibility IN ('Private','Public'));
ALTER TABLE rooms ADD COLUMN closed boolean NOT NULL DEFAULT false;
ALTER TABLE rooms ADD COLUMN match_id text REFERENCES matches(id);
UPDATE rooms r SET host_account=COALESCE(fp_account,sh_account),
    room_name=a.username || '''s game',
    match_id=NULLIF(state->>'matchId','')
FROM accounts a WHERE a.id=COALESCE(r.fp_account,r.sh_account);
UPDATE rooms r SET state=state || jsonb_build_object(
    'hostAccount',host_account,'hostName',a.username,'name',room_name,
    'visibility','Private','closed',false,'managed',false,'invitationEnabled',true)
FROM accounts a WHERE a.id=r.host_account;
CREATE INDEX rooms_public_open ON rooms(id COLLATE "C")
    WHERE visibility='Public' AND NOT closed AND match_id IS NULL AND (fp_account IS NULL OR sh_account IS NULL);
ALTER TABLE matches ADD COLUMN display_name text NOT NULL DEFAULT '';
UPDATE matches m SET display_name=r.room_name FROM rooms r WHERE r.match_id=m.id;
