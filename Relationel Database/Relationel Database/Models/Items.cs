namespace Relationel_Database.Models;

public class Item
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int ItemID { get; internal set; }
}
