-- ROLES: who may see and do what.
--
--   developer    full access. (still cant see passwords since they are hashed)
--   game_server  what the C# game server needs, nothing more (rule of least privilege).
--   moderator    read-only, PLAYER data only. No game content(avoid leaks, unreleased items f.eks), no email(GDPR).

CREATE ROLE developer   LOGIN PASSWORD 'devpassword';
CREATE ROLE game_server LOGIN PASSWORD 'gamepassword';
CREATE ROLE moderator   LOGIN PASSWORD 'modpassword';

-- DEVELOPER: full access
GRANT ALL PRIVILEGES ON ALL TABLES    IN SCHEMA public TO developer;
GRANT USAGE, SELECT ON ALL SEQUENCES  IN SCHEMA public TO developer;

-- GAME_SERVER: may read everything...
GRANT SELECT ON ALL TABLES IN SCHEMA public TO game_server;
-- ... but may only create and change PLAYER data. Game content is changed by developers.
GRANT INSERT, UPDATE, DELETE
    ON account, character, guild, inventory, character_mount, character_quest
    TO game_server;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO game_server;

-- MODERATOR: investigates players, so PLAYER data only (gold, level, inventory,
-- mounts, quest progress, guild). Game content tables (item, item_type, mount,
-- quest, quest_reward, inventory_type, realm) are NOT granted, so unreleased content
-- cannot leak. The moderator sees ids (item 4, mount 2), not names.
GRANT SELECT ON character, guild, inventory, character_mount, character_quest TO moderator;

-- for account, column-level GRANT: id and hash, but never the email.
-- "SELECT email FROM account" and "SELECT * FROM account" are refused. (GDPR)
GRANT SELECT (account_id, password_hash) ON account TO moderator;
