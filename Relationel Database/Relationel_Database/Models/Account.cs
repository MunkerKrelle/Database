namespace Relationel_Database.Models;

public class Account
{
    public int AccountID { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    // Navigation
    public List<Character> Characters { get; set; } = new();

    public static Account Create(string email, string password) => new()
    {
        Email = email.Trim().ToLowerInvariant(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
    };
}
