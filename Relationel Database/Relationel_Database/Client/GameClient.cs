using Relationel_Database.Server;

namespace Relationel_Database.Client;

public class GameClient
{
    private readonly GameServer _server;
    private int? _accountId; // set after a successful login

    public GameClient(GameServer server) => _server = server;

    public void SignUp(string email, string password)
    {
        bool created = _server.SignUp(email, password);
        Console.WriteLine(created ? $"Signed up {email}" : $"Sign up failed: {email} is already taken");
    }

    public void Login(string email, string password)
    {
        _accountId = _server.Login(email, password);
        Console.WriteLine(_accountId != null ? $"Logged in as {email}" : $"Login failed for {email}");
    }

    public void CreateCharacter(string name, int realmId)
    {
        if (_accountId == null)
        {
            Console.WriteLine("Cannot create a character: not logged in");
            return;
        }

        bool created = _server.CreateCharacter(_accountId.Value, name, realmId);
        Console.WriteLine(created ? $"Created character {name}" : $"The name {name} is already taken on this realm");
    }

    public void BuyItem(int buyerId, int sellerId, int inventoryTypeId, int slotNo, int price)
    {
        bool bought = _server.BuyItem(buyerId, sellerId, inventoryTypeId, slotNo, price);
        Console.WriteLine(bought ? "Trade saved (COMMIT)" : "Trade failed, nothing was saved (ROLLBACK)");
    }
}
