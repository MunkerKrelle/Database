using Relationel_Database.Models;

namespace RelationelDatabase.Models;

public class Inventory
{
    public int PlayerID { get; set; }
    public int ItemID { get; set; }
    public int Quantity { get; set; }

    public Player? Player { get; set; }
    public Item? Item { get; set; }
}
