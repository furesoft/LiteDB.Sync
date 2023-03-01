namespace  Example;

public static class Program
{
    public static void Main()
    {
        var db = new LiteDB.Sync.SyncedLiteDatabase(".\\test.db");
        db.AfterSync += () =>
        {
            var users = db.GetCollection<User>().FindAll();

            foreach (var user in users)
            {
                Console.WriteLine(user.Username);
            }
        };

        db.Start(Guid.Parse("B3559E46-3458-4A97-835E-EA8FE6FB00C6")); //id to identify your database


        Console.WriteLine("Enter Username: ");
        var username = Console.ReadLine();

        db.BeginTrans();

        db.GetCollection<User>().Insert(new User {Username = username});

        db.Commit(); // commit triggers sync

        var users = db.GetCollection<User>().FindAll();

        foreach (var user in users)
        {
            Console.WriteLine(user.Username);
        }
    }
}