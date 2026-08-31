using RelationelDatabase.Data;
using RelationelDatabase.Models;

Console.WriteLine("Connecting to database...");

using var db = new AppDbContext();

// Read all players
Console.WriteLine("Players:");
foreach (var p in db.Players)
{
    Console.WriteLine($"ID: {p.PlayerID}, Name: {p.Name}");
}

// Insert new player
db.Players.Add(new Player { Name = "NewPlayer" });
db.SaveChanges();

// Read inventory for player 1
Console.WriteLine("\nInventory for Player 1:");
var inventory = db.Inventory
    .Where(i => i.PlayerID == 1)
    .Join(db.Items,
        inv => inv.ItemID,
        item => item.ItemID,
        (inv, item) => new { item.Name, inv.Quantity });

foreach (var entry in inventory)
{
    Console.WriteLine($"{entry.Name} x{entry.Quantity}");
}

Console.WriteLine("\nDone.");
