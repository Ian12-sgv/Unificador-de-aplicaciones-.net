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
    public class TomaFisicaRepository
    {
        private string connectionString;

        public TomaFisicaRepository()
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
        public async Task<IEnumerable<TomaFisicaMae>> Get(string imei)
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try
                {
                    string sql = "";
                    sql = string.Format("EXECUTE  [dbo].[ApiBuscarTomaFisicaMae] '{0}'", imei);
                    dbConnection.Open();
                    return dbConnection.Query<TomaFisicaMae>(sql);
                }
                catch(Exception fail)
                {
                    log.Error(fail.Message);
                    return null;
                }

            }
        }
        public async Task<IEnumerable<TomaFisicaMae>> GetPost(int numeroPagina, string codigoBarra,string imei)
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try
                {
                    string sql = "";
                    sql = string.Format("EXECUTE  [dbo].ApiBuscarTomaFisicaPost {0}, '{1}', '{2}'", numeroPagina, codigoBarra, imei);
                    dbConnection.Open();
                    return dbConnection.Query<TomaFisicaMae>(sql);
                }
                catch(Exception fail)
                {
                    log.Error(fail.Message);
                    return null;
                }
               
            }
        }


        public int Create(TomaFisicaMae data)
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try
                {
                    dbConnection.Open();
                    //dbConnection.BeginTransaction();
                    var dbparams = new DynamicParameters();
                    dbparams.Add("@CodigoBarra", data.CodigoBarra);
                    dbparams.Add("@NumeroPagina", data.NumeroPagina);
                    dbparams.Add("@Observacion", data.Observacion);
                    dbparams.Add("@Cantidad", data.Cantidad);
                    dbparams.Add("@Valor", data.Valor);
                    dbparams.Add("@Imei", data.Imei);
                    // var result = Connection.Execute("EXECUTE [dbo].[ApiProcesarToma] ", dbparams);
                    var result = dbConnection.Execute("[dbo].[ApiProcesarToma]", param: dbparams, commandType: CommandType.StoredProcedure);
                    //dbConnection.Close();
                    return result;
                }
                catch(Exception fail)
                {
                    log.Error(fail.Message);
                    return -1;
                }

            }
        }

    }
}
