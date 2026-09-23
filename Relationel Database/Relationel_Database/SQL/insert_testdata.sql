-- ACCOUNTS
INSERT INTO account (email, password_hash) VALUES
('peter@example.com',  '$2a$11$mz5mEv/xjDRzD26vucMo4.3wDJQXRhlbeGJZdmyNokCUG9CKtwEy.'),
('kasper@example.com', '$2a$11$TmKDJa56i/CwkTRWSw8gK.TAUUHdcTqbu4H12JGVPi1f.VG1c402K');

-- REALMS
INSERT INTO realm (realm_name) VALUES
('Azeroth'),
('Northrend');

-- GUILDS
INSERT INTO guild (guild_name, realm_id) VALUES
('Warriors of Light', 1),
('Shadow Clan', 2),
('Destroyers', 1),
('Mages United', 2),
('Dark Brotherhood', 1),
('Knights of Valor', 2),
('Miracle Guild', 1),
('Legends of the North', 2),
('Crimson Blades', 1),
('Eternal Guardians', 2);

-- CHARACTERS
INSERT INTO character (char_name, level, gold, account_id, realm_id, guild_id)
VALUES
('PeterWarrior', 10, 150, 1, 1, 1),
('KasperMage', 20, 300, 2, 2, 2);

-- ITEM TYPES (the type decides how many fit in one slot)
INSERT INTO item_type (type_name, max_stack) VALUES
('Weapon', 1),
('Off-hand', 1),
('Chestpiece', 1),
('Headwear', 1),
('Footwear', 1),
('Potion', 20),
('Food', 20),
('Misc', 50);

-- ITEMS (name, quality, item type)
INSERT INTO item (item_name, quality, item_type_id) VALUES
('Sword', 'common', 1),        -- Weapon
('Shield', 'common', 2),       -- Off-hand
('Potion', 'uncommon', 6),     -- Potion, stacks to 20
('Epic Staff', 'epic', 1);     -- Weapon

-- INVENTORY TYPES
INSERT INTO inventory_type (type_name, slot_count) VALUES
('Backpack', 20),
('Bank', 28),
('Pockets', 4);

-- INVENTORY (character, inventory type, slot, item, amount)
INSERT INTO inventory (character_id, inventory_type_id, slot_no, item_id, amount) VALUES
(1, 1, 1, 1, 1),   -- Peter, Backpack slot 1: Sword
(1, 2, 1, 3, 2),   -- Peter, Bank slot 1: 2 Potions
(1, 1, 2, 2, 1),   -- Peter, Backpack slot 2: Shield
(2, 1, 1, 3, 5),   -- Kasper, Backpack slot 1: 5 Potions
(2, 1, 2, 4, 1);   -- Kasper, Backpack slot 2: Epic Staff

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
