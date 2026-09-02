namespace Relationel_Database.Models;

public class Inventory
{
    public int InventoryID { get; set; }

    public int PlayerID { get; set; }
    public required Player Player { get; set; }

    public int ItemID { get; set; }
    public required Item Item { get; set; }

    public int Quantity { get; set; }
}

