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

        public SqlConnection Connection
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
            using (SqlConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                IDbTransaction transaction = null;
                try
                {
                    dbConnection.Open();
                    transaction = dbConnection.BeginTransaction();
                    var dbparams = new DynamicParameters();
                    dbparams.Add("@CodigoBarra", data.CodigoBarra);
                    dbparams.Add("@NumeroPagina", data.NumeroPagina);
                    dbparams.Add("@Observacion", data.Observacion);
                    dbparams.Add("@Cantidad", data.Cantidad);
                    dbparams.Add("@Valor", data.Valor);
                    dbConnection.Execute(
                        "[dbo].[ApiProcesarToma]",
                        param: dbparams,
                        transaction: transaction,
                        commandType: CommandType.StoredProcedure);

                    const string actualizarAutor = @"
UPDATE dbo.TOMAFISICA1
SET autor = @Autor
WHERE NumeroPagina = @NumeroPagina;";

                    var filasActualizadas = dbConnection.Execute(
                        actualizarAutor,
                        new { Autor = data.Autor, NumeroPagina = data.NumeroPagina },
                        transaction: transaction);

                    if (filasActualizadas == 0)
                        throw new InvalidOperationException("No se encontro la Toma Fisica 1 para asignar el autor.");

                    transaction.Commit();
                    return 1;
                }
                catch(Exception fail)
                {
                    if (transaction != null)
                    {
                        try { transaction.Rollback(); }
                        catch (Exception rollbackFail) { log.Error(rollbackFail.Message); }
                    }

                    log.Error(fail.Message);
                    return -1;
                }

            }
        }

    }
}
