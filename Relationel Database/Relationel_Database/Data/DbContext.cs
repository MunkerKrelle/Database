using Microsoft.EntityFrameworkCore;
using Relationel_Database.Models;

namespace Relationel_Database.Data;

public class AppDbContext : DbContext
{
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Inventory> Inventory => Set<Inventory>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "CONNECTION_STRING environment variable is not set.");
        }

        Console.WriteLine($"Using connection string: {connectionString}");

        optionsBuilder.UseNpgsql(
            connectionString,
            options => options.EnableRetryOnFailure()
        );
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Player>()
            .ToTable("players")
            .Property(p => p.PlayerID)
            .HasColumnName("playerid");

        modelBuilder.Entity<Player>()
            .Property(p => p.Name)
            .HasColumnName("name");

        modelBuilder.Entity<Item>()
            .ToTable("items")
            .Property(i => i.ItemID)
            .HasColumnName("itemid");

        modelBuilder.Entity<Item>()
            .Property(i => i.Name)
            .HasColumnName("name");

        modelBuilder.Entity<Inventory>()
            .ToTable("inventory")
            .Property(i => i.InventoryID)
            .HasColumnName("inventoryid");

        modelBuilder.Entity<Inventory>()
            .Property(i => i.PlayerID)
            .HasColumnName("playerid");

        modelBuilder.Entity<Inventory>()
            .Property(i => i.ItemID)
            .HasColumnName("itemid");

        modelBuilder.Entity<Inventory>()
            .Property(i => i.Quantity)
            .HasColumnName("quantity");

        modelBuilder.Entity<Inventory>()
            .HasKey(i => new { i.PlayerID, i.ItemID });

        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.Player)
            .WithMany()
            .HasForeignKey(i => i.PlayerID);

        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.Item)
            .WithMany()
            .HasForeignKey(i => i.ItemID);
    }
}