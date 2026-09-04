-- ACCOUNTS
INSERT INTO account (email, password) VALUES
('peter@example.com', '1234'),
('kasper@example.com', 'abcd');

-- REALMS
INSERT INTO realm (realm_name) VALUES
('Azeroth'),
('Northrend');

-- GUILDS
INSERT INTO guild (guild_name, realm_id) VALUES
('Warriors of Light', 1),
('Shadow Clan', 2);

-- CHARACTERS
INSERT INTO character (char_name, level, gold, account_id, realm_id, guild_id)
VALUES
('PeterWarrior', 10, 150, 1, 1, 1),
('KasperMage', 20, 300, 2, 2, 2);

-- ITEMS
INSERT INTO item (item_name, quality, item_type) VALUES
('Sword', 'Common', 'Weapon'),
('Shield', 'Common', 'Armor'),
('Potion', 'Uncommon', 'Consumable'),
('Epic Staff', 'Epic', 'Weapon');

-- INVENTORY
INSERT INTO inventory (character_id, slot_no, item_id, amount, location) VALUES
(1, 1, 1, 1, 0),
(1, 2, 2, 1, 0),
(2, 1, 3, 5, 0),
(2, 2, 4, 1, 0);

-- MOUNTS
INSERT INTO mount (mount_name, speed, is_flying) VALUES
('Swift Horse', 60, false),
('Bronze Dragon', 120, true);

-- CHARACTER_MOUNT (collected mounts)
INSERT INTO character_mount (character_id, mount_id, collected_date) VALUES
(1, 1, NOW()),
(2, 2, NOW());

-- QUESTS
INSERT INTO quest (title, xp_reward) VALUES
('Defeat the Goblin King', 500),
('Collect 10 Herbs', 150);

-- CHARACTER_QUEST (accepted quests)
INSERT INTO character_quest (character_id, quest_id, is_completed) VALUES
(1, 1, true),
(2, 2, false);

-- QUEST_REWARD (quest gives items)
INSERT INTO quest_reward (quest_id, item_id) VALUES
(1, 4),  -- Goblin King gives Epic Staff
(2, 3);  -- Herb quest gives Potion
