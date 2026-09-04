namespace Relationel_Database.Models;

public class QuestReward
{
    public int QuestID { get; set; }
    public Quest? Quest { get; set; }

    public int ItemID { get; set; }
    public Item? Item { get; set; }
}

