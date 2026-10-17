CREATE TABLE match_reads (
    match_id text NOT NULL REFERENCES matches(id) ON DELETE CASCADE,
    account_id text NOT NULL,
    revision bigint NOT NULL CHECK (revision >= 0),
    PRIMARY KEY (match_id, account_id)
);

CREATE INDEX matches_fp_page ON matches(fp_account, id COLLATE "C");
CREATE INDEX matches_sh_page ON matches(sh_account, id COLLATE "C");
