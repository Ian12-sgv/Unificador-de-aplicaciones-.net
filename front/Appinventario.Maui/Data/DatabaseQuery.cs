using Appinventario.Models;
using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Appinventario.Data
{
    public class DatabaseQuery
    {
        readonly SQLiteAsyncConnection _database;
        readonly Task _initializationTask;

        public DatabaseQuery(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _initializationTask = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await _database.CreateTableAsync<Parameter>();
            await EnsureCurrentSharedListenerAsync();
        }

        private async Task EnsureCurrentSharedListenerAsync()
        {
            const string currentListener = "192.168.0.135:5001";
            const string previousListener = "192.168.0.246";

            var parameter = await _database.Table<Parameter>().OrderBy(x => x.Id).FirstOrDefaultAsync();

            if (parameter == null)
            {
                await _database.InsertAsync(new Parameter
                {
                    IP = currentListener,
                    IDIMEI = false
                });
                return;
            }

            if (string.IsNullOrWhiteSpace(parameter.IP) ||
                parameter.IP.Contains(previousListener))
            {
                parameter.IP = currentListener;
                await _database.UpdateAsync(parameter);
            }
        }

        private Task EnsureInitializedAsync()
        {
            return _initializationTask;
        }

        #region CRUD

        //SELECT
        public async Task<List<Parameter>> GetParameters()
        {
            await EnsureInitializedAsync();
            return await _database.QueryAsync<Parameter>("SELECT * FROM Parameter ORDER BY id LIMIT 1 ");
        }

        public async Task<Parameter> GetParameterAsync()
        {
            await EnsureInitializedAsync();
            return await _database.Table<Parameter>().OrderBy(x => x.Id).FirstOrDefaultAsync();
        }

        //INSERT UPDATE
        public async Task<int> SaveParameterAsync(Parameter parameter)
        {
            await EnsureInitializedAsync();

            if (parameter.Id != 0)
                return await _database.UpdateAsync(parameter);
            else
                return await _database.InsertAsync(parameter);
        }

        public Task<int> SaveParameterAsync(ref Parameter parameter)
        {
            return SaveParameterAsync(parameter);
        }
        #endregion

        //Agregado por angel
        public async Task<List<Parameter>> GeTarameyteAng()
        {
            await EnsureInitializedAsync();
            return await _database.Table<Parameter>().ToListAsync();
        }
    }
}

