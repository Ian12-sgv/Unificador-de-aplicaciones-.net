using Dapper;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using WebServicesApiNetCor.Models;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor.Data
{
    public class TomaFisicaDetalleRepository
    {

        private string connectionString;
        public TomaFisicaDetalleRepository()
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

        public async Task<IEnumerable<TomaFisicaDet>> Get(int numeroPagina, string imei)
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try
                {
                    string sql = string.Format("EXECUTE  [dbo].[ApiBuscarTomaFisicaDetalle] '{0}','{1}'", numeroPagina, imei);
                    dbConnection.Open();
                    return dbConnection.Query<TomaFisicaDet>(sql);
                }
                catch (Exception fail)
                {
                    log.Error(fail.Message);
                    return null;
                }
            }
        }

        public async Task<IEnumerable<TomaFisicaDet>> GetTotal(int numeroPagina, string estacion)
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try
                {
                    string sql = string.Format("EXECUTE  [dbo].[ApiBuscarTomaFisicaTotal] '{0}','{1}'", numeroPagina, estacion);
                    dbConnection.Open();
                    return dbConnection.Query<TomaFisicaDet>(sql);
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

