namespace Relationel_Database.Models;

public class Character
{
    public int CharId { get; set; }
    public string CharName { get; set; } = "";
    public int Level { get; set; } = 1;
    public int Gold { get; set; }

    public int AccountID { get; set; }
    public Account? Account { get; set; }

    public int RealmID { get; set; }
    public Realm? Realm { get; set; }

    public int? GuildID { get; set; }
    public Guild? Guild { get; set; }

    public int? RidingMountID { get; set; }
    public Mount? RidingMount { get; set; }

    public List<CharacterQuest> AcceptedQuests { get; set; } = new();
    public List<CharacterMount> CollectedMounts { get; set; } = new();
    public List<Inventory> Inventory { get; set; } = new();
}

