using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Relationel_Database.Models;

namespace Relationel_Database.Data;

public class AppDbContext : DbContext
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Character> Characters => Set<Character>();
    public DbSet<Realm> Realms => Set<Realm>();
    public DbSet<Guild> Guilds => Set<Guild>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Inventory> Inventory => Set<Inventory>();
    public DbSet<Mount> Mounts => Set<Mount>();
    public DbSet<Quest> Quests => Set<Quest>();
    public DbSet<CharacterQuest> CharacterQuests => Set<CharacterQuest>();
    public DbSet<CharacterMount> CharacterMounts => Set<CharacterMount>();
    public DbSet<QuestReward> QuestRewards => Set<QuestReward>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("CONNECTION_STRING environment variable is not set.");

        optionsBuilder
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .LogTo(Console.WriteLine, LogLevel.Error);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- TABLE NAMES ---
        modelBuilder.Entity<Character>().ToTable("character");
        modelBuilder.Entity<Account>().ToTable("account");
        modelBuilder.Entity<Realm>().ToTable("realm");
        modelBuilder.Entity<Guild>().ToTable("guild");
        modelBuilder.Entity<Item>().ToTable("item");
        modelBuilder.Entity<Inventory>().ToTable("inventory");
        modelBuilder.Entity<Mount>().ToTable("mount");
        modelBuilder.Entity<Quest>().ToTable("quest");
        modelBuilder.Entity<CharacterQuest>().ToTable("character_quest");
        modelBuilder.Entity<CharacterMount>().ToTable("character_mount");
        modelBuilder.Entity<QuestReward>().ToTable("quest_reward");

        // CHARACTER_QUEST
        modelBuilder.Entity<CharacterQuest>()
            .Property(cq => cq.CharacterID)
            .HasColumnName("character_id");

        modelBuilder.Entity<CharacterQuest>()
            .Property(cq => cq.QuestID)
            .HasColumnName("quest_id");

        modelBuilder.Entity<CharacterQuest>()
            .HasKey(cq => new { cq.CharacterID, cq.QuestID });

        modelBuilder.Entity<CharacterQuest>()
            .HasOne(cq => cq.Character)
            .WithMany(c => c.AcceptedQuests)
            .HasForeignKey(cq => cq.CharacterID);

        modelBuilder.Entity<CharacterQuest>()
            .HasOne(cq => cq.Quest)
            .WithMany(q => q.AcceptedBy)
            .HasForeignKey(cq => cq.QuestID);

        // CHARACTER_MOUNT
        modelBuilder.Entity<CharacterMount>()
            .Property(cm => cm.CharacterID)
            .HasColumnName("character_id");

        modelBuilder.Entity<CharacterMount>()
            .Property(cm => cm.MountID)
            .HasColumnName("mount_id");

        modelBuilder.Entity<CharacterMount>()
            .HasKey(cm => new { cm.CharacterID, cm.MountID });

        modelBuilder.Entity<CharacterMount>()
            .HasOne(cm => cm.Character)
            .WithMany(c => c.CollectedMounts)
            .HasForeignKey(cm => cm.CharacterID);

        modelBuilder.Entity<CharacterMount>()
            .HasOne(cm => cm.Mount)
            .WithMany()
            .HasForeignKey(cm => cm.MountID);

        // QUEST_REWARD
        modelBuilder.Entity<QuestReward>()
            .Property(qr => qr.QuestID)
            .HasColumnName("quest_id");

        modelBuilder.Entity<QuestReward>()
            .Property(qr => qr.ItemID)
            .HasColumnName("item_id");

        modelBuilder.Entity<QuestReward>()
            .HasKey(qr => new { qr.QuestID, qr.ItemID });

        modelBuilder.Entity<QuestReward>()
            .HasOne(qr => qr.Quest)
            .WithMany(q => q.Rewards)
            .HasForeignKey(qr => qr.QuestID);

        modelBuilder.Entity<QuestReward>()
            .HasOne(qr => qr.Item)
            .WithMany()
            .HasForeignKey(qr => qr.ItemID);

        // CHARACTER riding mount (1:1)
        modelBuilder.Entity<Character>()
            .Property(c => c.RidingMountID)
            .HasColumnName("riding_mount_id");

        modelBuilder.Entity<Character>()
            .HasOne(c => c.RidingMount)
            .WithOne()
            .HasForeignKey<Character>(c => c.RidingMountID);

        // INVENTORY
        modelBuilder.Entity<Inventory>()
            .Property(i => i.CharacterID)
            .HasColumnName("character_id");

        modelBuilder.Entity<Inventory>()
            .Property(i => i.SlotNo)
            .HasColumnName("slot_no");

        modelBuilder.Entity<Inventory>()
            .Property(i => i.ItemID)
            .HasColumnName("item_id");

        modelBuilder.Entity<Inventory>()
            .Property(i => i.Amount)
            .HasColumnName("amount");

        modelBuilder.Entity<Inventory>()
            .Property(i => i.Location)
            .HasColumnName("location");

        modelBuilder.Entity<Inventory>()
            .HasKey(i => new { i.CharacterID, i.SlotNo });

        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.Character)
            .WithMany(c => c.Inventory)
            .HasForeignKey(i => i.CharacterID);

        modelBuilder.Entity<Inventory>()
            .HasOne(i => i.Item)
            .WithMany()
            .HasForeignKey(i => i.ItemID);
    }
}
