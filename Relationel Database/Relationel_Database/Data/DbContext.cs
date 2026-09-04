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
        
        // ACCOUNT
        modelBuilder.Entity<Account>(e =>
        {
            e.ToTable("account");

            e.HasKey(a => a.AccountID);
            e.Property(a => a.AccountID).HasColumnName("account_id");
            e.Property(a => a.Email).HasColumnName("email");
            e.Property(a => a.Password).HasColumnName("password");

            e.HasMany(a => a.Characters)
             .WithOne(c => c.Account)
             .HasForeignKey(c => c.AccountID);
        });

        
        // REALM
        modelBuilder.Entity<Realm>(e =>
        {
            e.ToTable("realm");

            e.HasKey(r => r.RealmID);
            e.Property(r => r.RealmID).HasColumnName("realm_id");
            e.Property(r => r.RealmName).HasColumnName("realm_name");

            e.HasMany(r => r.Characters)
             .WithOne(c => c.Realm)
             .HasForeignKey(c => c.RealmID);

            e.HasMany(r => r.Guilds)
             .WithOne(g => g.Realm)
             .HasForeignKey(g => g.RealmID);
        });

        
        // GUILD
        modelBuilder.Entity<Guild>(e =>
        {
            e.ToTable("guild");

            e.HasKey(g => g.GuildID);
            e.Property(g => g.GuildID).HasColumnName("guild_id");
            e.Property(g => g.GuildName).HasColumnName("guild_name");
            e.Property(g => g.RealmID).HasColumnName("realm_id");

            e.HasMany(g => g.Members)
             .WithOne(c => c.Guild)
             .HasForeignKey(c => c.GuildID);
        });

        
        // MOUNT
        modelBuilder.Entity<Mount>(e =>
        {
            e.ToTable("mount");

            e.HasKey(m => m.MountID);
            e.Property(m => m.MountID).HasColumnName("mount_id");
            e.Property(m => m.MountName).HasColumnName("mount_name");
            e.Property(m => m.Speed).HasColumnName("speed");
            e.Property(m => m.IsFlying).HasColumnName("is_flying");
        });

        
        // CHARACTER
        modelBuilder.Entity<Character>(e =>
        {
            e.ToTable("character");

            e.HasKey(c => c.CharId);
            e.Property(c => c.CharId).HasColumnName("char_id");
            e.Property(c => c.CharName).HasColumnName("char_name");
            e.Property(c => c.Level).HasColumnName("level");
            e.Property(c => c.Gold).HasColumnName("gold");

            e.Property(c => c.AccountID).HasColumnName("account_id");
            e.Property(c => c.RealmID).HasColumnName("realm_id");
            e.Property(c => c.GuildID).HasColumnName("guild_id");
            e.Property(c => c.RidingMountID).HasColumnName("riding_mount_id");

            e.HasOne(c => c.RidingMount)
             .WithOne()
             .HasForeignKey<Character>(c => c.RidingMountID);
        });

        
        // ITEM
        
        modelBuilder.Entity<Item>(e =>
        {
            e.ToTable("item");

            e.HasKey(i => i.ItemID);
            e.Property(i => i.ItemID).HasColumnName("item_id");
            e.Property(i => i.ItemName).HasColumnName("item_name");
            e.Property(i => i.Quality).HasColumnName("quality");
            e.Property(i => i.ItemType).HasColumnName("item_type");
        });

        
        // INVENTORY
        modelBuilder.Entity<Inventory>(e =>
        {
            e.ToTable("inventory");

            e.HasKey(i => new { i.CharacterID, i.SlotNo });

            e.Property(i => i.CharacterID).HasColumnName("character_id");
            e.Property(i => i.SlotNo).HasColumnName("slot_no");
            e.Property(i => i.ItemID).HasColumnName("item_id");
            e.Property(i => i.Amount).HasColumnName("amount");
            e.Property(i => i.Location).HasColumnName("location");

            e.HasOne(i => i.Character)
             .WithMany(c => c.Inventory)
             .HasForeignKey(i => i.CharacterID);

            e.HasOne(i => i.Item)
             .WithMany()
             .HasForeignKey(i => i.ItemID);
        });

        
        // QUEST
        modelBuilder.Entity<Quest>(e =>
        {
            e.ToTable("quest");

            e.HasKey(q => q.QuestID);
            e.Property(q => q.QuestID).HasColumnName("quest_id");
            e.Property(q => q.Title).HasColumnName("title");
            e.Property(q => q.XPReward).HasColumnName("xp_reward");
        });

        // -- JOINED TABLES --

        // CHARACTER_QUEST
        modelBuilder.Entity<CharacterQuest>(e =>
        {
            e.ToTable("character_quest");

            e.HasKey(cq => new { cq.CharacterID, cq.QuestID });

            e.Property(cq => cq.CharacterID).HasColumnName("character_id");
            e.Property(cq => cq.QuestID).HasColumnName("quest_id");

            e.HasOne(cq => cq.Character)
             .WithMany(c => c.AcceptedQuests)
             .HasForeignKey(cq => cq.CharacterID);

            e.HasOne(cq => cq.Quest)
             .WithMany(q => q.AcceptedBy)
             .HasForeignKey(cq => cq.QuestID);
        });

        
        // CHARACTER_MOUNT
        modelBuilder.Entity<CharacterMount>(e =>
        {
            e.ToTable("character_mount");

            e.HasKey(cm => new { cm.CharacterID, cm.MountID });

            e.Property(cm => cm.CharacterID).HasColumnName("character_id");
            e.Property(cm => cm.MountID).HasColumnName("mount_id");
            e.Property(cm => cm.CollectedDate).HasColumnName("collected_date");

            e.HasOne(cm => cm.Character)
             .WithMany(c => c.CollectedMounts)
             .HasForeignKey(cm => cm.CharacterID);

            e.HasOne(cm => cm.Mount)
             .WithMany()
             .HasForeignKey(cm => cm.MountID);
        });

        
        // QUEST_REWARD
        modelBuilder.Entity<QuestReward>(e =>
        {
            e.ToTable("quest_reward");

            e.HasKey(qr => new { qr.QuestID, qr.ItemID });

            e.Property(qr => qr.QuestID).HasColumnName("quest_id");
            e.Property(qr => qr.ItemID).HasColumnName("item_id");

            e.HasOne(qr => qr.Quest)
             .WithMany(q => q.Rewards)
             .HasForeignKey(qr => qr.QuestID);

            e.HasOne(qr => qr.Item)
             .WithMany()
             .HasForeignKey(qr => qr.ItemID);
        });
    }

}