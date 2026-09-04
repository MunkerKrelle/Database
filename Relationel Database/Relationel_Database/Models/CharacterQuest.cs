namespace Relationel_Database.Models;

public class CharacterQuest
{
    public int CharacterID { get; set; }
    public Character? Character { get; set; }

    public int QuestID { get; set; }
    public Quest? Quest { get; set; }
}
