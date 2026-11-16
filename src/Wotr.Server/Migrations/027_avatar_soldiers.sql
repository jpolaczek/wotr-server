-- AVATAR-03 (owner, 2026-11-17): the Eye and the Ring are no longer avatars. Every account that has one gets one of the
-- eight nations' soldiers at random, the column's check allows only the eight, and its default (for a technical account's
-- plain insert) is a soldier drawn the same way.
UPDATE accounts SET avatar = (ARRAY['gondor', 'rohan', 'elves', 'dwarves', 'north', 'sauron', 'isengard', 'southrons'])
    [1 + floor(random() * 8)::int]
WHERE avatar IN ('eye', 'ring');
ALTER TABLE accounts DROP CONSTRAINT accounts_avatar_check;
ALTER TABLE accounts ADD CONSTRAINT accounts_avatar_check CHECK (avatar IN
    ('gondor', 'rohan', 'elves', 'dwarves', 'north', 'sauron', 'isengard', 'southrons'));
ALTER TABLE accounts ALTER COLUMN avatar SET DEFAULT
    (ARRAY['gondor', 'rohan', 'elves', 'dwarves', 'north', 'sauron', 'isengard', 'southrons'])[1 + floor(random() * 8)::int];
