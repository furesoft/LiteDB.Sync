using LiteDB;

namespace Example;

public class User
{
    [BsonId]
    public ObjectId _id { get; set; }
    
    public string Username { get; set; }
    public string Password { get; set; }
}