namespace Relationel_Database.Models;

public class Quest
{
    public int QuestID { get; set; }
    public string Title { get; set; } = "";
    public int XPReward { get; set; }

    public List<CharacterQuest> AcceptedBy { get; set; } = new();
    public List<QuestReward> Rewards { get; set; } = new();
}


