using System.Linq.Expressions;
using System.Reflection;
using Ipfs;
using Ipfs.Http;
using LiteDB.Engine;
using OwlCore.Kubo;
using OwlCore.Storage;

namespace LiteDB.Sync;

public class SyncedLiteDatabase  : ILiteRepository
{
    private ILiteRepository _liteRepositoyImplementation;
    private KuboBootstrapper _bootstrapper;
    private PeerRoom _room;
    public string Filename { get; }

    public SyncedLiteDatabase(string filename)
    {
        Filename = filename;

        _liteRepositoyImplementation = new LiteRepository(filename);
    }


    public void Start(Guid guid)
    {
        IFile kuboBinary = GetKuboBinary().Result;

        var repoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), guid.ToString());
        _bootstrapper = new(kuboBinary, repoPath);
        
        Task.WaitAll(_bootstrapper.StartAsync());
        
        var ipfsClient = new IpfsClient(_bootstrapper.ApiUri.ToString());
        
        _room = new(ipfsClient.IdAsync().Result, ipfsClient.PubSub, guid.ToString());
        _room.MessageReceived += MessageReceived;

        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            _room.Dispose();
            _bootstrapper.Stop();
        };
        
        //Todo: on start request current database version
    }

    public event Action AfterSync;
    
    private void MessageReceived(object? sender, IPublishedMessage e)
    {
        //ToDo: apply sync changes
        var msg = SyncMessage.From(e.DataBytes);

        switch (msg.State)
        {
            
        }
        
        AfterSync?.Invoke();
    }

    public void Dispose()
    {
        _liteRepositoyImplementation.Dispose();

        _bootstrapper.Dispose();
        
        Directory.Delete(_bootstrapper.BinaryWorkingFolder.ToString(), true);
    }

    private static async Task<IFile> GetKuboBinary()
    {
        var downloader = new KuboDownloader();

        var latestKuboBinary = await downloader.DownloadLatestBinaryAsync();

        return latestKuboBinary;
    }

    private SyncMessage SerializeMessage<T>(T entity, string collectionName = null)
    {
        var serializedEntity = BsonMapper.Global.Serialize(entity);
        var binary = BsonSerializer.Serialize(serializedEntity.AsDocument);
        
        return new() { CollectionName = collectionName, ObjectData = binary };
    }

    private void SyncChange<T>(T entity, ChangeState state, string collectionName = null)
    {
        var msg = SerializeMessage(entity, collectionName);
        msg.State = state;

        Task.WaitAll(_room.PublishAsync(msg));
    }
    
    public void Insert<T>(T entity, string collectionName = null)
    {
        SyncChange(entity, ChangeState.Insert, collectionName);

        _liteRepositoyImplementation.Insert(entity, collectionName);
    }

    public int Insert<T>(IEnumerable<T> entities, string collectionName = null)
    {
        foreach (var entity in entities)
        {
            Insert(entity, collectionName);
        }

        return entities.Count();
    }

    public bool Update<T>(T entity, string collectionName = null)
    {
        SyncChange(entity, ChangeState.Update, collectionName);

        return _liteRepositoyImplementation.Update(entity, collectionName);
    }

    public int Update<T>(IEnumerable<T> entities, string collectionName = null)
    {
        foreach (var entity in entities)
        {
            Update(entity, collectionName);
        }

        return entities.Count();
    }

    public bool Upsert<T>(T entity, string collectionName = null)
    {
        SyncChange(entity, ChangeState.Upsert, collectionName);
        
        return _liteRepositoyImplementation.Upsert(entity, collectionName);
    }

    public int Upsert<T>(IEnumerable<T> entities, string collectionName = null)
    {
        foreach (var entity in entities)
        {
            Upsert(entity, collectionName);
        }

        return entities.Count();
    }

    public bool Delete<T>(BsonValue id, string collectionName = null)
    {
        SyncChange(id, ChangeState.Delete, collectionName);
        
        return _liteRepositoyImplementation.Delete<T>(id, collectionName);
    }

    public int DeleteMany<T>(BsonExpression predicate, string collectionName = null)
    {
        var idsToDelete = _liteRepositoyImplementation
            .Fetch<T>(predicate, collectionName)
            .Select(entity=> GetIdOfEntity(entity))
            .Select(id => (BsonValue)id);

        var bsonArr = new BsonArray(idsToDelete);
        
        SyncChange(bsonArr, ChangeState.DeleteMany, collectionName);
        
        return idsToDelete.Count();
    }

    private object GetIdOfEntity<T>(T entity)
    {
        return entity
            .GetType()
            .GetProperties()
            .First(_ => _.GetCustomAttribute<BsonIdAttribute>() != null)
            .GetValue(entity);
    }

    public int DeleteMany<T>(Expression<Func<T, bool>> predicate, string collectionName = null)
    {
        var idsToDelete = _liteRepositoyImplementation
            .Fetch<T>(predicate, collectionName)
            .Select(entity=> GetIdOfEntity(entity))
            .Select(id => (BsonValue)id);

        var bsonArr = new BsonArray(idsToDelete);
        
        SyncChange(bsonArr, ChangeState.DeleteMany, collectionName);

        return idsToDelete.Count();
    }

    public ILiteQueryable<T> Query<T>(string collectionName = null)
    {
        return _liteRepositoyImplementation.Query<T>(collectionName);
    }

    public bool EnsureIndex<T>(string name, BsonExpression expression, bool unique = false, string collectionName = null)
    {
        return _liteRepositoyImplementation.EnsureIndex<T>(name, expression, unique, collectionName);
    }

    public bool EnsureIndex<T>(BsonExpression expression, bool unique = false, string collectionName = null)
    {
        return _liteRepositoyImplementation.EnsureIndex<T>(expression, unique, collectionName);
    }

    public bool EnsureIndex<T, K>(Expression<Func<T, K>> keySelector, bool unique = false, string collectionName = null)
    {
        return _liteRepositoyImplementation.EnsureIndex<T, K>(keySelector, unique, collectionName);
    }

    public bool EnsureIndex<T, K>(string name, Expression<Func<T, K>> keySelector, bool unique = false, string collectionName = null)
    {
        return _liteRepositoyImplementation.EnsureIndex<T, K>(name, keySelector, unique, collectionName);
    }

    public T SingleById<T>(BsonValue id, string collectionName = null)
    {
        return _liteRepositoyImplementation.SingleById<T>(id, collectionName);
    }

    public List<T> Fetch<T>(BsonExpression predicate, string collectionName = null)
    {
        return _liteRepositoyImplementation.Fetch<T>(predicate, collectionName);
    }

    public List<T> Fetch<T>(Expression<Func<T, bool>> predicate, string collectionName = null)
    {
        return _liteRepositoyImplementation.Fetch<T>(predicate, collectionName);
    }

    public T First<T>(BsonExpression predicate, string collectionName = null)
    {
        return _liteRepositoyImplementation.First<T>(predicate, collectionName);
    }

    public T First<T>(Expression<Func<T, bool>> predicate, string collectionName = null)
    {
        return _liteRepositoyImplementation.First<T>(predicate, collectionName);
    }

    public T FirstOrDefault<T>(BsonExpression predicate, string collectionName = null)
    {
        return _liteRepositoyImplementation.FirstOrDefault<T>(predicate, collectionName);
    }

    public T FirstOrDefault<T>(Expression<Func<T, bool>> predicate, string collectionName = null)
    {
        return _liteRepositoyImplementation.FirstOrDefault<T>(predicate, collectionName);
    }

    public T Single<T>(BsonExpression predicate, string collectionName = null)
    {
        return _liteRepositoyImplementation.Single<T>(predicate, collectionName);
    }

    public T Single<T>(Expression<Func<T, bool>> predicate, string collectionName = null)
    {
        return _liteRepositoyImplementation.Single<T>(predicate, collectionName);
    }

    public T SingleOrDefault<T>(BsonExpression predicate, string collectionName = null)
    {
        return _liteRepositoyImplementation.SingleOrDefault<T>(predicate, collectionName);
    }

    public T SingleOrDefault<T>(Expression<Func<T, bool>> predicate, string collectionName = null)
    {
        return _liteRepositoyImplementation.SingleOrDefault<T>(predicate, collectionName);
    }

    public ILiteDatabase Database { get; }
}