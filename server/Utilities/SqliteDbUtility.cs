using System.Data.SQLite;
using server.Models;

namespace server.Utilities
{
    public class SqliteDbUtility
    {
        private string ConnectionString { get; set; } = null!;

        public SqliteDbUtility() { }

        public static SqliteDbUtility GetInstance(string connectionString)
        {
            return new SqliteDbUtility { ConnectionString = connectionString };
        }

        public async Task CreateTableAsync(string tableDefinition)
        {
            // try
            // {
            using SQLiteConnection connection = new(ConnectionString);
            connection.Open();

            using SQLiteCommand createCommand = new(tableDefinition, connection);
            await createCommand.ExecuteNonQueryAsync();

            // }
            // catch { }
        }

        public async Task<List<Doc>> GetAllDocsAsync(string collection)
        {
            using SQLiteConnection connection = new(ConnectionString);
            connection.Open();

            // Check if table exists
            using SQLiteCommand command = new($"SELECT * from Doc where collection=@collection", connection);
            command.Parameters.Add(new SQLiteParameter("@collection", collection));

            List<Doc> docs = new();
            using SQLiteDataReader reader = command.ExecuteReader();
            while (await reader.ReadAsync())
            {
                docs.Add(new Doc(reader[0].ToString() ?? "", reader[1].ToString() ?? "", reader[2].ToString() ?? "", reader[3] is DBNull ? null : reader[3].ToString()));
            }
            return docs;
        }

        public async Task<Doc?> GetAsync(string collection, string key)
        {
            using SQLiteConnection connection = new(ConnectionString);
            connection.Open();

            // Check if table exists
            using SQLiteCommand command = new($"SELECT * from Doc where collection=@collection and key=@key", connection);
            command.Parameters.Add(new SQLiteParameter("@collection", collection));
            command.Parameters.Add(new SQLiteParameter("@key", key));

            using SQLiteDataReader reader = command.ExecuteReader();
            while (await reader.ReadAsync())
            {
                return new Doc(reader[0].ToString() ?? "", reader[1].ToString() ?? "", reader[2].ToString() ?? "", reader[3].ToString());
            }
            return null;
        }

        public async Task<Doc> UpsertAsync(string collection, string key, string description, string? location)
        {
            var isDoc = (await GetAsync(collection, key)) is null;

            using SQLiteConnection connection = new(ConnectionString);
            connection.Open();

            if (isDoc)
            {
                using SQLiteCommand command = new($"INSERT into Doc (collection, key, description, location) values (@collection, @key, @description, @location)", connection);
                command.Parameters.Add(new SQLiteParameter("@collection", collection));
                command.Parameters.Add(new SQLiteParameter("@key", key));
                command.Parameters.Add(new SQLiteParameter("@description", description));
                if (location is null)
                {
                    command.Parameters.Add(new SQLiteParameter("@location", DBNull.Value));
                }
                else
                {
                    command.Parameters.Add(new SQLiteParameter("@location", location));
                }
                await command.ExecuteNonQueryAsync();
            }
            else
            {
                using SQLiteCommand command = new($"UPDATE Doc set description=@description and location=@location", connection);
                command.Parameters.Add(new SQLiteParameter("@description", description));
                if (location is null)
                {
                    command.Parameters.Add(new SQLiteParameter("@location", DBNull.Value));
                }
                else
                {
                    command.Parameters.Add(new SQLiteParameter("@location", location));
                }
                await command.ExecuteNonQueryAsync();
            }
            return new Doc(collection, key, description, location);
        }

        public async Task<int> DeleteAsync(string collection, string key)
        {
            using SQLiteConnection connection = new(ConnectionString);
            connection.Open();
            using SQLiteCommand command = new($"DELETE from Doc where collection=@collection and key=@key", connection);
            command.Parameters.Add(new SQLiteParameter("@collection", collection));
            command.Parameters.Add(new SQLiteParameter("@key", key));
            return await command.ExecuteNonQueryAsync();
        }
    }
}
