-- ACCOUNT
CREATE TABLE account (
    account_id SERIAL PRIMARY KEY,
    email VARCHAR(255) NOT NULL,
    password VARCHAR(255) NOT NULL
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
    realm_id INT NOT NULL REFERENCES realm(realm_id)
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
    gold INT NOT NULL DEFAULT 0,

    account_id INT NOT NULL REFERENCES account(account_id),
    realm_id INT NOT NULL REFERENCES realm(realm_id),
    guild_id INT REFERENCES guild(guild_id),

    riding_mount_id INT REFERENCES mount(mount_id)
);

-- ITEM
CREATE TABLE item (
    item_id SERIAL PRIMARY KEY,
    item_name VARCHAR(100) NOT NULL,
    quality VARCHAR(50) NOT NULL,
    item_type VARCHAR(50) NOT NULL
);

-- INVENTORY
CREATE TABLE inventory (
    character_id INT NOT NULL REFERENCES character(char_id),
    slot_no INT NOT NULL,
    item_id INT NOT NULL REFERENCES item(item_id),
    amount INT NOT NULL DEFAULT 1,
    location INT NOT NULL,

    PRIMARY KEY (character_id, slot_no)
);

-- CHARACTER_MOUNT (M:N collected mounts)
CREATE TABLE character_mount (
    character_id INT NOT NULL REFERENCES character(char_id),
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
    character_id INT NOT NULL REFERENCES character(char_id),
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
