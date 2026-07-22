using Appinventario.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
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
            await EnsureParameterColumnsAsync();
        }

        private Task EnsureInitializedAsync()
        {
            return _initializationTask;
        }

        private async Task EnsureParameterColumnsAsync()
        {
            var columns = await _database.GetTableInfoAsync(nameof(Parameter));

            if (!columns.Any(c => string.Equals(c.Name, nameof(Parameter.BorradorDevUrl), StringComparison.OrdinalIgnoreCase)))
            {
                await _database.ExecuteAsync("ALTER TABLE Parameter ADD COLUMN BorradorDevUrl TEXT NULL");
            }
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
