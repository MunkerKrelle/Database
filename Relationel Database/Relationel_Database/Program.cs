using Microsoft.EntityFrameworkCore;
using Relationel_Database.Data;
using Relationel_Database.Models;

Console.WriteLine("Connecting to database...");

try
{
    using var db = new AppDbContext();
    Console.WriteLine("Tables exist: " + db.Database.CanConnect());

    // Read all players
    Console.WriteLine("Players:");

    Console.WriteLine(db.Players.ToQueryString());

    var entityType = db.Model.FindEntityType(typeof(Player));
    Console.WriteLine(entityType.GetTableName());


    foreach (var p in db.Players)
    {
        Console.WriteLine($"ID: {p.PlayerID}, Name: {p.Name}");
    }

    // Insert new player
    db.Players.Add(new Player
    {
        Name = "NewPlayer"
    });

    db.SaveChanges();

    // Read inventory for player 1
    Console.WriteLine("\nInventory for Player 1:");

    var inventory = db.Inventory
        .Where(i => i.PlayerID == 1)
        .Join(
            db.Items,
            inv => inv.ItemID,
            item => item.ItemID,
            (inv, item) => new
            {
                item.Name,
                inv.Quantity
            });

    foreach (var entry in inventory)
    {
        Console.WriteLine($"{entry.Name} x{entry.Quantity}");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Database error:");
    Console.WriteLine(ex);

}

Console.WriteLine("\nDone.");