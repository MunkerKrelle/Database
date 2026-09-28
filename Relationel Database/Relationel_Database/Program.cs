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

    // Transaction
    client.BuyItem(buyerId: 3, sellerId: 1, inventoryTypeId: 1, slotNo: 2, price: 50);

    Console.WriteLine();

    // Replication
    Console.WriteLine("Replication:");
    using var standby = new AppDbContext("STANDBY_CONNECTION_STRING");
    Console.WriteLine("Standby has MadsPaladin: " + standby.Characters.Any(c => c.CharName == "MadsPaladin"));
    Console.WriteLine();

    // ------------------------------------------------------------
    // GENERATE TEST DATA
    // ------------------------------------------------------------

    if (db.Accounts.Count() < 100)
    {
        GenerateTestData(db, characterCount: 50000);
    }
    else
    {
        Console.WriteLine("Test data already exists - skipping generation.\n");
    }

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

    #region Queries written for relational algebra in the exam slides ---
        Console.WriteLine("Rewards still waiting (first 20):");
        var waitingRewards =
            (from cq in db.CharacterQuests
             join qr in db.QuestRewards on cq.QuestID equals qr.QuestID
             where !cq.IsCompleted
             select new { cq.Character!.CharName, qr.Item!.ItemName })
            .Take(20);
        foreach (var r in waitingRewards)
            Console.WriteLine($"Character: {r.CharName}, Reward: {r.ItemName}");
        Console.WriteLine();

        Console.WriteLine("Gold per guild:");
        var guildGold = db.Characters
            .Where(c => c.GuildID != null)
            .GroupBy(c => new { c.GuildID, c.Guild!.GuildName })
            .Select(g => new { Guild = g.Key.GuildName, TotalGold = g.Sum(c => c.Gold) })
            .Where(g => g.TotalGold > 200);
        foreach (var g in guildGold)
            Console.WriteLine($"Guild: {g.Guild}, Total gold: {g.TotalGold}");
        Console.WriteLine();
    #endregion
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

        int guildId = guildsByRealm.TryGetValue(realmId, out var guildIds) && guildIds.Count > 0
            ? guildIds[random.Next(guildIds.Count)]
            : 0;

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