using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Relationel_Database.Client;
using Relationel_Database.Data;
using Relationel_Database.Models;
using Relationel_Database.Server;


try
{
    using var db = new AppDbContext();

    Console.WriteLine("Database connected: " + db.Database.CanConnect());
    Console.WriteLine("--------------------------------------------");


    // --- PSEUDO CLIENT / SERVER ---
    // The client (the player's PC) has no database access at all. It talks to the
    // server, and only the server talks to Postgres, as the role game_server defined in roles.sql
    // the following is a few calls that show how the pieces could fit together, in a rough theoretic sense
    var server = new GameServer();
    var client = new GameClient(server);

    Console.WriteLine("Player session:");
    client.SignUp("mads@gmail.com", "12kode34");    // password travels client -> server, server only the hash+salt
    client.Login("mads@gmail.com", "kode1234");           // wrong password
    client.Login("mads@gmail.com", "12kode34");           // correct password
    client.CreateCharacter("MadsPaladin", realmId: 1);       // character creation; needs a login
    Console.WriteLine();


    // --- DEVELOPER VIEW: look straight into the database ---

    // ACCOUNTS
    Console.WriteLine("Accounts:");
    foreach (var acc in db.Accounts)
        Console.WriteLine($"ID: {acc.AccountID}, Email: {acc.Email}");
    Console.WriteLine();

    // REALMS
    Console.WriteLine("Realms:");
    foreach (var r in db.Realms)
        Console.WriteLine($"ID: {r.RealmID}, Name: {r.RealmName}");
    Console.WriteLine();

    // GUILDS
    Console.WriteLine("Guilds:");
    foreach (var g in db.Guilds.Include(g => g.Realm))
        Console.WriteLine($"ID: {g.GuildID}, Name: {g.GuildName}, Realm: {g.Realm?.RealmName}");
    Console.WriteLine();

    // CHARACTERS
    Console.WriteLine("Characters:");
    foreach (var c in db.Characters
        .Include(c => c.Account)
        .Include(c => c.Realm)
        .Include(c => c.Guild))
    {
        Console.WriteLine(
            $"ID: {c.CharId}, Name: {c.CharName}, Level: {c.Level}, Gold: {c.Gold}, " +
            $"Account: {c.Account?.Email}, Realm: {c.Realm?.RealmName}, Guild: {c.Guild?.GuildName}"
        );
    }
    Console.WriteLine();

    // ITEM TYPES
    Console.WriteLine("Item types:");
    foreach (var t in db.ItemTypes)
        Console.WriteLine($"ID: {t.ItemTypeID}, Name: {t.TypeName}, Max stack: {t.MaxStack}");
    Console.WriteLine();

    // ITEMS
    Console.WriteLine("Items:");
    foreach (var item in db.Items.Include(i => i.ItemType))
        Console.WriteLine($"ID: {item.ItemID}, Name: {item.ItemName}, Quality: {item.Quality}, Type: {item.ItemType?.TypeName}");
    Console.WriteLine();

    //
    // INVENTORY TYPES
    //
    Console.WriteLine("Inventory types:");
    foreach (var t in db.InventoryTypes)
        Console.WriteLine($"ID: {t.InventoryTypeID}, Name: {t.TypeName}, Slots: {t.SlotCount}");
    Console.WriteLine();

    //
    // INVENTORY
    //
    Console.WriteLine("Inventory:");
    foreach (var inv in db.Inventory
        .Include(i => i.Character)
        .Include(i => i.InventoryType)
        .Include(i => i.Item)
        .OrderBy(i => i.CharacterID).ThenBy(i => i.InventoryTypeID).ThenBy(i => i.SlotNo))
    {
        Console.WriteLine(
            $"Character: {inv.Character?.CharName}, Inventory: {inv.InventoryType?.TypeName}, Slot: {inv.SlotNo}, Item: {inv.Item?.ItemName}, Amount: {inv.Amount}"
        );
    }
    Console.WriteLine();

    //
    // MOUNTS
    //
    Console.WriteLine("Mounts:");
    foreach (var m in db.Mounts)
        Console.WriteLine($"ID: {m.MountID}, Name: {m.MountName}, Speed: {m.Speed}, Flying: {m.IsFlying}");
    Console.WriteLine();

    //
    // CHARACTER_MOUNT (collected mounts)
    //
    Console.WriteLine("Collected mounts:");
    foreach (var cm in db.CharacterMounts
        .Include(cm => cm.Character)
        .Include(cm => cm.Mount))
    {
        Console.WriteLine(
            $"Character: {cm.Character?.CharName}, Mount: {cm.Mount?.MountName}, Collected: {cm.CollectedDate}"
        );
    }
    Console.WriteLine();

    //
    // QUESTS
    //
    Console.WriteLine("Quests:");
    foreach (var q in db.Quests)
        Console.WriteLine($"ID: {q.QuestID}, Title: {q.Title}, XP: {q.XPReward}");
    Console.WriteLine();

    //
    // CHARACTER_QUEST (accepted quests)
    //
    Console.WriteLine("Accepted quests:");
    foreach (var cq in db.CharacterQuests
        .Include(cq => cq.Character)
        .Include(cq => cq.Quest))
    {
        Console.WriteLine(
            $"Character: {cq.Character?.CharName}, Quest: {cq.Quest?.Title}, Completed: {cq.IsCompleted}"
        );
    }
    Console.WriteLine();

    //
    // QUEST_REWARD (quest rewards items)
    //
    Console.WriteLine("Quest rewards:");
    foreach (var qr in db.QuestRewards
        .Include(qr => qr.Quest)
        .Include(qr => qr.Item))
    {
        Console.WriteLine(
            $"Quest: {qr.Quest?.Title}, Reward Item: {qr.Item?.ItemName}"
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
