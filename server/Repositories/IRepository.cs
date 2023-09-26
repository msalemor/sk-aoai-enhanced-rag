namespace server.Repositories
{
    public interface IRepository<T>
    {
        Task<List<T>> GetAllDocsAsync(string collection);
        Task<T?> GetAsync(string collection, string key);

        Task<T> UpsertAsync(string collection, string key, string description, string? location);
        Task<int> DeleteAsync(string collection, string key);
    }
}