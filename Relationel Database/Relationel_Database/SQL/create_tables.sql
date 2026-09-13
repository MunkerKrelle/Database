-- ACCOUNT
CREATE TABLE account (
    account_id SERIAL PRIMARY KEY,
    email VARCHAR(255) NOT NULL UNIQUE
        CHECK (email ~* '^[^@\s]+@[^@\s]+\.[^@\s]+$'),
        
    password_hash VARCHAR(255) NOT NULL
);

-- REALM
CREATE TABLE realm (
    realm_id SERIAL PRIMARY KEY,
    realm_name VARCHAR(100) NOT NULL
);

-- GUILD
CREATE TABLE guild (
    guild_id SERIAL PRIMARY KEY,
    guild_name VARCHAR(100) NOT NULL,
    realm_id INT NOT NULL REFERENCES realm(realm_id),

    UNIQUE (guild_id, realm_id)
);

-- MOUNT
CREATE TABLE mount (
    mount_id SERIAL PRIMARY KEY,
    mount_name VARCHAR(100) NOT NULL,
    speed INT NOT NULL,
    is_flying BOOLEAN NOT NULL
);

-- CHARACTER
CREATE TABLE character (
    char_id SERIAL PRIMARY KEY,
    char_name VARCHAR(100) NOT NULL,
    level INT NOT NULL DEFAULT 1,
    gold INT NOT NULL DEFAULT 0 CHECK (gold >= 0),

    account_id INT NOT NULL REFERENCES account(account_id),
    realm_id INT NOT NULL REFERENCES realm(realm_id),
    guild_id INT,

    riding_mount_id INT REFERENCES mount(mount_id) ON DELETE SET NULL,
    
    UNIQUE (char_name, realm_id),
    FOREIGN KEY (guild_id, realm_id) REFERENCES guild (guild_id, realm_id)
        ON DELETE SET NULL (guild_id)
);

CREATE FUNCTION check_riding_mount_owned() RETURNS trigger AS $$
BEGIN
    IF NEW.riding_mount_id IS NULL THEN
        RETURN NEW;
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM character_mount
        WHERE character_id = NEW.char_id
          AND mount_id = NEW.riding_mount_id
    ) THEN
        RAISE EXCEPTION 'Character % has not collected mount %',
            NEW.char_id, NEW.riding_mount_id;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;


CREATE TRIGGER trg_check_riding_mount_owned
    BEFORE INSERT OR UPDATE OF riding_mount_id ON character
    FOR EACH ROW
    EXECUTE FUNCTION check_riding_mount_owned();


CREATE TYPE item_quality AS ENUM ('common', 'uncommon', 'rare', 'epic', 'legendary');

-- ITEM_TYPE (Weapon, off-hand, footwear, Potion, ...)
CREATE TABLE item_type (
    item_type_id SERIAL PRIMARY KEY,
    type_name VARCHAR(50) NOT NULL UNIQUE,
    max_stack INT NOT NULL DEFAULT 1
);

-- ITEM
CREATE TABLE item (
    item_id SERIAL PRIMARY KEY,
    item_name VARCHAR(100) NOT NULL,
    quality item_quality NOT NULL,
    item_type_id INT NOT NULL REFERENCES item_type(item_type_id)
);

-- INVENTORY_TYPE (Backpack, Bank, Pockets, ...)
CREATE TABLE inventory_type (
    inventory_type_id SERIAL PRIMARY KEY,
    type_name VARCHAR(50) NOT NULL UNIQUE,
    slot_count INT NOT NULL
);

-- INVENTORY
CREATE TABLE inventory (
    character_id INT NOT NULL REFERENCES character(char_id) ON DELETE CASCADE,
    inventory_type_id INT NOT NULL REFERENCES inventory_type(inventory_type_id),
    slot_no INT NOT NULL,
    item_id INT NOT NULL REFERENCES item(item_id),
    amount INT NOT NULL DEFAULT 1 CHECK (amount > 0),

    PRIMARY KEY (character_id, inventory_type_id, slot_no)
);

CREATE FUNCTION check_inventory_row() RETURNS trigger AS $$
DECLARE
    max_slots  INT;
    max_amount INT;
BEGIN
    SELECT slot_count INTO max_slots
    FROM inventory_type
    WHERE inventory_type_id = NEW.inventory_type_id;

    IF NEW.slot_no < 1 OR NEW.slot_no > max_slots THEN
        RAISE EXCEPTION 'Slot % does not exist: inventory type % has % slots',
            NEW.slot_no, NEW.inventory_type_id, max_slots;
    END IF;

    SELECT t.max_stack INTO max_amount
    FROM item i
    JOIN item_type t ON t.item_type_id = i.item_type_id
    WHERE i.item_id = NEW.item_id;

    IF NEW.amount > max_amount THEN
        RAISE EXCEPTION 'Amount % too high: item % stacks to at most %',
            NEW.amount, NEW.item_id, max_amount;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_check_inventory_row
    BEFORE INSERT OR UPDATE ON inventory
    FOR EACH ROW
    EXECUTE FUNCTION check_inventory_row();


CREATE TABLE character_mount (
    character_id INT NOT NULL REFERENCES character(char_id) ON DELETE CASCADE,
    mount_id INT NOT NULL REFERENCES mount(mount_id),
    collected_date TIMESTAMP NOT NULL,

    PRIMARY KEY (character_id, mount_id)
);

-- QUEST
CREATE TABLE quest (
    quest_id SERIAL PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    xp_reward INT NOT NULL
);

-- CHARACTER_QUEST (M:N accepted quests)
CREATE TABLE character_quest (
    character_id INT NOT NULL REFERENCES character(char_id) ON DELETE CASCADE,
    quest_id INT NOT NULL REFERENCES quest(quest_id),
    is_completed BOOLEAN NOT NULL DEFAULT FALSE,

    PRIMARY KEY (character_id, quest_id)
);

-- QUEST_REWARD (M:N quest rewards items)
CREATE TABLE quest_reward (
    quest_id INT NOT NULL REFERENCES quest(quest_id),
    item_id INT NOT NULL REFERENCES item(item_id),

    PRIMARY KEY (quest_id, item_id)
);
