using Microsoft.EntityFrameworkCore;
using Npgsql;
using Relationel_Database.Client;
using Relationel_Database.Data;
using Relationel_Database.Models;
using Relationel_Database.Server;

try
{
    using var db = new AppDbContext();

    Console.WriteLine("Database connected: " + db.Database.CanConnect());
    Console.WriteLine("--------------------------------------------");

    // --- PSEUDO CLIENT / SERVER (as you already had) ---
    var server = new GameServer();
    var client = new GameClient(server);

    Console.WriteLine("Player session:");
    client.SignUp("mads@gmail.com", "12kode34");
    client.Login("mads@gmail.com", "kode1234");   // wrong password
    client.Login("mads@gmail.com", "12kode34");   // correct password
    client.CreateCharacter("MadsPaladin", realmId: 1);
    Console.WriteLine();

    // --- GENERATE TEST DATA (run only once - skipped if data already exists) ---
    if (db.Accounts.Count() < 100)
        GenerateTestData(db, characterCount: 5000);
    else
        Console.WriteLine("Test data already exists - skipping generation.\n");

    // --- INDEX BENCHMARK: same 4 queries run with EXPLAIN ANALYZE, before and after indexes ---
    RunIndexBenchmark(db);

    // --- DEVELOPER VIEW (only the first 20 rows, so the console doesn't drown in 5000 characters) ---
    Console.WriteLine("Characters (first 20):");
    foreach (var c in db.Characters
        .Include(c => c.Account)
        .Include(c => c.Realm)
        .Include(c => c.Guild)
        .Take(20))
    {
        Console.WriteLine(
            $"ID: {c.CharId}, Name: {c.CharName}, Level: {c.Level}, Gold: {c.Gold}, " +
            $"Account: {c.Account?.Email}, Realm: {c.Realm?.RealmName}, Guild: {c.Guild?.GuildName}"
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

    Console.WriteLine($"Generating {characterCount} accounts...");
    var accounts = new List<Account>();
    for (int i = 0; i < characterCount; i++)
    {
        accounts.Add(new Account
        {
            Email = $"user{i}@example.com",
            PasswordHash = "$2a$11$mz5mEv/xjDRzD26vucMo4.3wDJQXRhlbeGJZdmyNokCUG9CKtwEy."
        });
    }
    db.Accounts.AddRange(accounts);
    db.SaveChanges(); // required so accounts get their AccountID

    Console.WriteLine($"Generating {characterCount} characters...");

    // A guild always belongs to one specific realm (character_guild_id_realm_id_fkey),
    // so guild_id can't just be picked independently of realm_id - we look up which
    // guilds actually exist per realm and only pick among those.
    var guildsByRealm = db.Guilds
        .AsEnumerable()
        .GroupBy(g => g.RealmID)
        .ToDictionary(g => g.Key, g => g.Select(x => x.GuildID).ToList());

    var characters = new List<Character>();
    foreach (var acc in accounts)
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
            CharName = $"Char_{acc.AccountID}",
            Level = random.Next(1, 81),
            Gold = random.Next(0, 10000),
            AccountID = acc.AccountID,
            RealmID = realmId,
            GuildID = guildId
        });
    }
    db.Characters.AddRange(characters);
    db.SaveChanges(); // required so characters get their CharId

    Console.WriteLine("Generating inventory rows...");
    var inventoryRows = new List<Inventory>();
    foreach (var ch in characters)
    {
        for (int slot = 1; slot <= 3; slot++)
        {
            inventoryRows.Add(new Inventory
            {
                CharacterID = ch.CharId,
                InventoryTypeID = ((slot - 1) % 3) + 1, // matches slot -> no PK conflict
                SlotNo = slot,
                ItemID = random.Next(1, 5),
                Amount = random.Next(1, 6)
            });
        }
    }
    db.Inventory.AddRange(inventoryRows);
    db.SaveChanges();

    Console.WriteLine("Generating mounts and quests per character...");
    var mounts = new List<CharacterMount>();
    var quests = new List<CharacterQuest>();
    foreach (var ch in characters)
    {
        mounts.Add(new CharacterMount
        {
            CharacterID = ch.CharId,
            MountID = random.Next(1, 3),
            CollectedDate = DateTime.UtcNow
        });
        quests.Add(new CharacterQuest
        {
            CharacterID = ch.CharId,
            QuestID = random.Next(1, 3),
            IsCompleted = random.NextDouble() < 0.5
        });
    }
    db.CharacterMounts.AddRange(mounts);
    db.CharacterQuests.AddRange(quests);
    db.SaveChanges();

    Console.WriteLine(
        $"Test data generated: {accounts.Count} accounts, {characters.Count} characters, " +
        $"{inventoryRows.Count} inventory rows.\n"
    );
}

static void RunIndexBenchmark(AppDbContext db)
{
    (string Label, string Sql)[] queries =
    {
        ("Find character by name (point lookup)",
            "SELECT * FROM character WHERE char_name = 'Char_2500'"),

        ("Find all characters on one realm (filter)",
            "SELECT * FROM character WHERE realm_id = 1"),

        ("Find all inventory for one character (most frequent 'hot path')",
            "SELECT * FROM inventory WHERE character_id = 2500"),

        ("Find account by email (control group)",
            "SELECT * FROM account WHERE email = 'user2500@example.com'")
    };

    db.Database.OpenConnection();
    var conn = (NpgsqlConnection)db.Database.GetDbConnection();

    // game_server does not own the tables (by design - the app role shouldn't be
    // able to change the schema), and CREATE/DROP INDEX in Postgres always
    // requires table ownership - it can't be granted separately. So the DDL
    // steps below use a separate, more privileged connection instead.
    var adminConnectionString = Environment.GetEnvironmentVariable("ADMIN_CONNECTION_STRING")
        ?? throw new InvalidOperationException("ADMIN_CONNECTION_STRING environment variable is not set.");
    using var adminConn = new NpgsqlConnection(adminConnectionString);
    adminConn.Open();

    void Exec(string sql)
    {
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    void ExecAdmin(string sql)
    {
        using var cmd = new NpgsqlCommand(sql, adminConn);
        cmd.ExecuteNonQuery();
    }

    void Analyze()
    {
        Exec("ANALYZE account;");
        Exec("ANALYZE character;");
        Exec("ANALYZE inventory;");
    }

    void Explain(string label, string sql)
    {
        Console.WriteLine($"--- {label} ---");
        Console.WriteLine($"SQL: {sql}");
        using var cmd = new NpgsqlCommand($"EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT) {sql}", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            Console.WriteLine(reader.GetString(0));
        Console.WriteLine();
    }

    Console.WriteLine("################  PHASE 1: WITHOUT INDEXES  ################\n");
    ExecAdmin("DROP INDEX IF EXISTS idx_character_char_name;");
    ExecAdmin("DROP INDEX IF EXISTS idx_character_realm_id;");
    ExecAdmin("DROP INDEX IF EXISTS idx_inventory_character_id;");
    Analyze();
    foreach (var (label, sql) in queries)
        Explain(label, sql);

    Console.WriteLine("################  CREATING INDEXES  ################\n");
    ExecAdmin("CREATE INDEX idx_character_char_name ON character(char_name);");
    ExecAdmin("CREATE INDEX idx_character_realm_id ON character(realm_id);");
    ExecAdmin("CREATE INDEX idx_inventory_character_id ON inventory(character_id);");
    Analyze();

    Console.WriteLine("################  PHASE 2: WITH INDEXES  ################\n");
    foreach (var (label, sql) in queries)
        Explain(label, sql);
}