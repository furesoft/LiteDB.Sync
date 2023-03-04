using OwlCore.Extensions;

namespace  Example;

public static class Program
{
    public static void Main()
    {
        var db = new LiteDB.Sync.SyncedLiteDatabase("Filename=.\\test.db; Connection=shared;");
        db.AfterSync += () =>
        {
            var users = db.Query<User>().ToArray();

            foreach (var user in users)
            {
                Console.WriteLine(user.Username);
            }
        };

        db.Start(Guid.Parse("B3559E46-3458-4A97-835E-EA8FE6FB00C6")); //id to identify your database


        Console.WriteLine("Enter Username: ");
        var username = Console.ReadLine();

        db.Insert(new User() { Username = username });

        while (true)
        {
            
        }
    }
}