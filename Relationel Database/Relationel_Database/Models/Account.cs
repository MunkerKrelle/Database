namespace Relationel_Database.Models;

public class Account
{
    public int AccountID { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    // Navigation
    public List<Character> Characters { get; set; } = new();
}
