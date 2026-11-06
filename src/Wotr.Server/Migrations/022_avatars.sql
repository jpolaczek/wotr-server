-- AVATAR-02 (AVATAR-01 A1, A2, A5): every account has one of ten avatars. Registration draws the Eye or the Ring; this gives
-- every existing account one the same way. The default keeps a technical account's plain insert working.
ALTER TABLE accounts ADD COLUMN avatar text NOT NULL DEFAULT 'eye' CHECK (avatar IN
    ('eye', 'ring', 'gondor', 'rohan', 'elves', 'dwarves', 'north', 'sauron', 'isengard', 'southrons'));
UPDATE accounts SET avatar = CASE WHEN random() < 0.5 THEN 'eye' ELSE 'ring' END;

-- The browser's account page holds its own short session, which reaches no game route.
ALTER TABLE sessions DROP CONSTRAINT sessions_purpose_check;
ALTER TABLE sessions ADD CONSTRAINT sessions_purpose_check CHECK (purpose IN ('game', 'admin', 'account'));
