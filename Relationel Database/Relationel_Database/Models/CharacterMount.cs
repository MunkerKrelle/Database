namespace Relationel_Database.Models;

public class CharacterMount
{
    public int CharacterID { get; set; }
    public Character? Character { get; set; }

    public int MountID { get; set; }
    public Mount? Mount { get; set; }

    public DateTime CollectedDate { get; set; }
}
