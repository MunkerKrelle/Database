namespace Relationel_Database.Models;

// Weapon, off-hand, footwear, Potion... Determines which, if any, armor slots an item is equipable, stackability etc...
public class ItemType
{
    public int ItemTypeID { get; set; }
    public string TypeName { get; set; } = "";
    public int MaxStack { get; set; } = 1;   // 1 = not stackable, 20 = can stack 20 of this in an inventory slot.
}
