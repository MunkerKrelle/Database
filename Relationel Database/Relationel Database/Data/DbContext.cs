using Microsoft.EntityFrameworkCore;
using Relationel_Database.Models;
using RelationelDatabase.Models;

namespace RelationelDatabase.Data;

public class AppDbContext : DbContext
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Inventory> Inventory => Set<Inventory>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var conn = Environment.GetEnvironmentVariable("CONNECTION_STRING")
                   ?? "Host=localhost;Port=5432;Database=inventory;Username=postgres;Password=yourpassword";

        options.UseNpgsql(conn);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inventory>()
            .HasKey(i => new { i.PlayerID, i.ItemID });
    }
}
