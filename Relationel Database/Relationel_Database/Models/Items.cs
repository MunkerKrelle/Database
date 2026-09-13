namespace Relationel_Database.Models;

public class Item
{
    public int ItemID { get; set; }
    public string ItemName { get; set; } = "";
    public ItemQuality Quality { get; set; }

    public int ItemTypeID { get; set; }
    public ItemType? ItemType { get; set; }
}
