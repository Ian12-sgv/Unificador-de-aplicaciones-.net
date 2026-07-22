using System.Data.SqlClient;
using System.Data;
using WebServicesApiNetCor.Util;
using Dapper;
using log4net;
using System;
using WebServicesApiNetCor.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebServicesApiNetCor.Data
{
    public class SucursalesAllRepository
    {

        private string connectionString;

        public SucursalesAllRepository()
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

        public async Task<IEnumerable<Sucursales>> Get(string CodigoBarra)
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try
                {
                    string sql = string.Format("SELECT * FROM  sucursales WHERE status = 1;");
                    dbConnection.Open();
                    return dbConnection.Query<Sucursales>(sql);
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
