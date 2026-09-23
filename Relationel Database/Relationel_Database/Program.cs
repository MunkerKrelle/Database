using Microsoft.EntityFrameworkCore;
using Npgsql;
using Relationel_Database.Analysis;
using Relationel_Database.Client;
using Relationel_Database.Data;
using Relationel_Database.Models;
using Relationel_Database.Server;

try
{
    using var db = new AppDbContext();

    Console.WriteLine("Database connected: " + db.Database.CanConnect());
    Console.WriteLine("--------------------------------------------");

    // ------------------------------------------------------------
    // PSEUDO CLIENT / SERVER
    // ------------------------------------------------------------

    var server = new GameServer();
    var client = new GameClient(server);

    Console.WriteLine("Player session:");

    client.SignUp("mads@gmail.com", "12kode34");
    client.Login("mads@gmail.com", "kode1234");   // Wrong password
    client.Login("mads@gmail.com", "12kode34");   // Correct password
    client.CreateCharacter("MadsPaladin", realmId: 1);

    Console.WriteLine();

    // ------------------------------------------------------------
    // GENERATE TEST DATA
    // ------------------------------------------------------------

    if (db.Accounts.Count() < 100)
    {
        GenerateTestData(db, characterCount: 5000);
    }
    else
    {
        Console.WriteLine("Test data already exists - skipping generation.\n");
    }

    // ------------------------------------------------------------
    // INDEX BENCHMARK
    //
    // The same four queries are executed twice:
    //
    // 1. Without the test indexes
    // 2. With the test indexes
    //
    // PostgreSQL EXPLAIN ANALYZE is used to show the query plan,
    // execution time, number of rows and buffer usage.
    // ------------------------------------------------------------

    IndexBenchmark.Run(db);

    // ------------------------------------------------------------
    // DEVELOPER VIEW
    // ------------------------------------------------------------

    Console.WriteLine("Characters (first 20):");

    foreach (var character in db.Characters
        .Include(c => c.Account)
        .Include(c => c.Realm)
        .Include(c => c.Guild)
        .Take(20))
    {
        Console.WriteLine(
            $"ID: {character.CharId}, " +
            $"Name: {character.CharName}, " +
            $"Level: {character.Level}, " +
            $"Gold: {character.Gold}, " +
            $"Account: {character.Account?.Email}, " +
            $"Realm: {character.Realm?.RealmName}, " +
            $"Guild: {character.Guild?.GuildName}"
        );
    }

    Console.WriteLine();
}
catch (Exception ex)
{
    Console.WriteLine("Database error:");
    Console.WriteLine(ex);
}

Console.WriteLine("\nDone.");


// ============================================================
// LOCAL FUNCTIONS
// ============================================================

static void GenerateTestData(AppDbContext db, int characterCount)
{
    var random = new Random();

    // ------------------------------------------------------------
    // Generate accounts
    // ------------------------------------------------------------

    Console.WriteLine($"Generating {characterCount} accounts...");

    var accounts = new List<Account>();

    for (int i = 0; i < characterCount; i++)
    {
        accounts.Add(new Account
        {
            Email = $"user{i}@example.com",

            // Test password hash
            PasswordHash =
                "$2a$11$mz5mEv/xjDRzD26vucMo4.3wDJQXRhlbeGJZdmyNokCUG9CKtwEy."
        });
    }

    db.Accounts.AddRange(accounts);

    // Save accounts first so that AccountID values are generated.
    db.SaveChanges();

    // ------------------------------------------------------------
    // Generate characters
    // ------------------------------------------------------------

    Console.WriteLine($"Generating {characterCount} characters...");

    // A guild belongs to a specific realm.
    // Therefore, the guild must be selected from the guilds
    // belonging to the character's selected realm.
    var guildsByRealm = db.Guilds
        .AsEnumerable()
        .GroupBy(g => g.RealmID)
        .ToDictionary(
            g => g.Key,
            g => g.Select(x => x.GuildID).ToList()
        );

    var characters = new List<Character>();

    foreach (var account in accounts)
    {
        int realmId = random.Next(1, 3);

        int? guildId = null;

        if (random.NextDouble() < 0.5
            && guildsByRealm.TryGetValue(realmId, out var guildOptions)
            && guildOptions.Count > 0)
        {
            guildId = guildOptions[random.Next(guildOptions.Count)];
        }

        characters.Add(new Character
        {
            CharName = $"Char_{account.AccountID}",
            Level = random.Next(1, 81),
            Gold = random.Next(0, 10000),

            AccountID = account.AccountID,
            RealmID = realmId,
            GuildID = guildId
        });
    }

    db.Characters.AddRange(characters);

    // Save characters first so that CharId values are generated.
    db.SaveChanges();

    // ------------------------------------------------------------
    // Generate inventory
    // ------------------------------------------------------------

    Console.WriteLine("Generating inventory rows...");

    var inventoryRows = new List<Inventory>();

    foreach (var character in characters)
    {
        for (int slot = 1; slot <= 3; slot++)
        {
            inventoryRows.Add(new Inventory
            {
                CharacterID = character.CharId,
                InventoryTypeID = ((slot - 1) % 3) + 1,
                SlotNo = slot,
                ItemID = random.Next(1, 5),
                Amount = 1
            });
        }
    }

    db.Inventory.AddRange(inventoryRows);
    db.SaveChanges();

    // ------------------------------------------------------------
    // Generate mounts and quests
    // ------------------------------------------------------------

    Console.WriteLine("Generating mounts and quests per character...");

    var mounts = new List<CharacterMount>();
    var quests = new List<CharacterQuest>();

    foreach (var character in characters)
    {
        mounts.Add(new CharacterMount
        {
            CharacterID = character.CharId,
            MountID = random.Next(1, 3),
            CollectedDate = DateTime.UtcNow
        });

        quests.Add(new CharacterQuest
        {
            CharacterID = character.CharId,
            QuestID = random.Next(1, 3),
            IsCompleted = random.NextDouble() < 0.5
        });
    }

    db.CharacterMounts.AddRange(mounts);
    db.CharacterQuests.AddRange(quests);

    db.SaveChanges();

    Console.WriteLine(
        $"Test data generated: " +
        $"{accounts.Count} accounts, " +
        $"{characters.Count} characters, " +
        $"{inventoryRows.Count} inventory rows.\n"
    );
}