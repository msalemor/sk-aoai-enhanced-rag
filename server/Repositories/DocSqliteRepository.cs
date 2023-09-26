using System.Data.Entity;
using server.Models;
using server.Services;

namespace server.Repositories;

public class DocSqliteRepository : IRepository<Doc>
{
    // public const string TABLE_DEFINITION = @"CREATE TABLE [IF NOT EXISTS] docs (
    // collection TEXT NOT NULL,
    // key TEXT NOT NULL,
    // description NOT NULL,
    // location TEXT NULL,
    // PRIMARY KEY (collection, key)) [WITHOUT ROWID]";

    public const string TABLE_DEFINITION = @"CREATE TABLE IF NOT EXISTS Doc (collection TEXT NOT NULL, key TEXT NOT NULL, description TEXT NOT NULL, location TEXT NULL, PRIMARY KEY(collection,key)) WITHOUT ROWID";


    public readonly SqliteDbService DatabaseUtil;

    public DocSqliteRepository(SqliteDbService dbUtil)
    {
        DatabaseUtil = dbUtil;
    }
    public async Task<int> DeleteAsync(string collection, string key)
    {
        return await DatabaseUtil.DeleteAsync(collection, key);
    }

    public async Task<List<Doc>> GetAllDocsAsync(string collection)
    {
        return await DatabaseUtil.GetAllDocsAsync(collection);
    }

    public async Task<Doc?> GetAsync(string collection, string key)
    {
        return await DatabaseUtil.GetAsync(collection, key);
    }

    public async Task<Doc> UpsertAsync(string collection, string key, string description, string? location)
    {
        return await DatabaseUtil.UpsertAsync(collection, key, description, location);
    }
}