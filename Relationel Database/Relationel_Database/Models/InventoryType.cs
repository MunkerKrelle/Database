namespace Relationel_Database.Models;

// Backpack, Bank, Pockets, ... A character has several separate inventories.
public class InventoryType
{
    public int InventoryTypeID { get; set; }
    public string TypeName { get; set; } = "";
    public int SlotCount { get; set; }
}
