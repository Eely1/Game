using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public class DatabaseHelper
    {
        readonly SQLiteAsyncConnection database;

        public DatabaseHelper(string dbPath)
        {
            database = new SQLiteAsyncConnection(dbPath);
            database.CreateTableAsync<Credential>().Wait();
        }

        public Task<List<Credential>> GetCredentialsAsync()
        {
            return database.Table<Credential>().ToListAsync();
        }

        public async Task SaveCredentialAsync(List<Credential> credentials)
        {
            var tasks = new List<Task>();

            foreach (var credential in credentials)
            {
                if (credential.ID != 0)
                {
                    tasks.Add(database.UpdateAsync(credential));
                }
                else
                {
                    tasks.Add(database.InsertAsync(credential));
                }
            }

            await Task.WhenAll(tasks);
        }
        public async Task<int> DeleteItemAsync(int credentialId)
        {
            return await database.DeleteAsync<Credential>(credentialId);
        }
    }
}
