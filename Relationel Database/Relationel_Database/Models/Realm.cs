namespace Relationel_Database.Models;

public class Realm
{
    public int RealmID { get; set; }
    public string RealmName { get; set; } = string.Empty;

    // Navigation
    public List<Character> Characters { get; set; } = new();
    public List<Guild> Guilds { get; set; } = new();
}
