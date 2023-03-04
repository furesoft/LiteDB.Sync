namespace LiteDB.Sync;

public enum ChangeState
{
    GetCurrent,
    Insert,
    Update,
    Delete,
    DeleteMany,
    Upsert
}