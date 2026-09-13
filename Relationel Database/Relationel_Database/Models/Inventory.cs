namespace Relationel_Database.Models;

public class Inventory
{
    public int CharacterID { get; set; }
    public Character? Character { get; set; }

    public int InventoryTypeID { get; set; }
    public InventoryType? InventoryType { get; set; }

    public int SlotNo { get; set; }
    public int ItemID { get; set; }
    public Item? Item { get; set; }

    public int Amount { get; set; }
}


