using Microsoft.EntityFrameworkCore;
using Relationel_Database.Data;
using Relationel_Database.Models;

namespace Relationel_Database.Server;

public class GameServer
{
    private readonly AppDbContext _db = new();

    public bool SignUp(string email, string password)
    {
        if (_db.Accounts.Any(a => a.Email == email))
            return false;

        _db.Accounts.Add(Account.Create(email, password));
        _db.SaveChanges();
        return true;
    }

    public int? Login(string email, string password)
    {
        var account = _db.Accounts.SingleOrDefault(a => a.Email == email);
        if (account == null)
            return null;

        // bcrypt keeps the salt inside the hash string: $2a$11$<22-char salt><31-char hash>
        // Verify reads the salt out of the stored hash, then hashes the user-typed password with
        // that same salt, and compares. That is why no separate salt column is needed.
        bool ok = BCrypt.Net.BCrypt.Verify(password, account.PasswordHash);
        return ok ? account.AccountID : null;
    }

    public bool CreateCharacter(int accountId, string name, int realmId)
    {
        if (_db.Characters.Any(c => c.RealmID == realmId && c.CharName == name))
            return false; // name allready taken on that realm

        _db.Characters.Add(new Character
        {
            CharName = name,
            AccountID = accountId,
            RealmID = realmId
        });
        _db.SaveChanges();
        return true;
    }

    public bool BuyItem(int buyerId, int sellerId, int inventoryTypeId, int slotNo, int price)
    {
        var slot = _db.Inventory.Where(i =>
            i.CharacterID == sellerId && i.InventoryTypeID == inventoryTypeId && i.SlotNo == slotNo);
        var seller = _db.Characters.Where(c => c.CharId == sellerId);
        var buyer = _db.Characters.Where(c => c.CharId == buyerId);

        using var transaction = _db.Database.BeginTransaction();
        try
        {
            if (slot.ExecuteUpdate(s => s.SetProperty(i => i.CharacterID, buyerId)) == 0)
                throw new InvalidOperationException("The seller has nothing in that slot");

            seller.ExecuteUpdate(s => s.SetProperty(c => c.Gold, c => c.Gold + price));

            buyer.ExecuteUpdate(s => s.SetProperty(c => c.Gold, c => c.Gold - price));

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            return false;
        }
    }
}
