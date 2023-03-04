namespace LiteDB.Sync;

public class SyncMessage
{
    public ChangeState State { get; set; }
    public string CollectionName { get; set; }
    public byte[] ObjectData { get; set; }
    
    public static implicit operator byte[](SyncMessage msg)
    {
        return BsonSerializer.Serialize(BsonMapper.Global.ToDocument(msg));
    }

    public static implicit operator BsonDocument(SyncMessage msg)
    {
        return BsonSerializer.Deserialize(msg.ObjectData);
    }

    public static SyncMessage From(byte[] buffer)
    {
        var document = BsonSerializer.Deserialize(buffer);

        return BsonMapper.Global.Deserialize<SyncMessage>(document);
    }
}