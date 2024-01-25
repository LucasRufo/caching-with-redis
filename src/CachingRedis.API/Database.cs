namespace CachingRedis.API;

public static class Database
{
    public static List<User> Db = [];

    public static async Task<List<User>> GetUsers()
    {
        await Task.Delay(2000);
        return Db;
    }

    public static async Task InsertUser(User user)
    {
        await Task.Delay(2000);
        Db.Add(user);
    }
}

public record User(string Name, string Email);
