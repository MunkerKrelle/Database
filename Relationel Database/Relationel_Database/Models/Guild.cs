namespace Relationel_Database.Models;

public class Guild
{
    public int GuildID { get; set; }
    public string GuildName { get; set; } = "";

    public int RealmID { get; set; }
    public Realm? Realm { get; set; }

    public List<Character> Members { get; set; } = new();
}