using Dapper;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using WebServicesApiNetCor.Models;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor.Data
{
    public class ValoresRepositorio
    {
        private string connectionString;
        public ValoresRepositorio()
        {
            connectionString = Modulo.connectionString;
        }
        public IDbConnection Connection
        {
            get
            {
                return new SqlConnection(connectionString);
            }
        }

        public async Task<IEnumerable<Valores>> Get()
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try
                {
                    string sql = string.Format("EXECUTE  [dbo].ApiBuscarValores");
                    dbConnection.Open();
                    return dbConnection.Query<Valores>(sql);
                }
                catch (Exception fail)
                {
                    log.Error(fail.Message);
                    return null;
                }


            }
        }

    }
}
