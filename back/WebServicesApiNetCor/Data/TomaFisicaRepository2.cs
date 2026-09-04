using Dapper;
using log4net;
using System;
using System.Data;
using System.Data.SqlClient;
using WebServicesApiNetCor.Models;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor.Data
{
    public class TomaFisicaRepository2
    {
        private readonly string connectionString = Modulo.connectionString;

        public int Create(TomaFisicaMae2 data)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                IDbTransaction transaction = null;

                try
                {
                    connection.Open();
                    transaction = connection.BeginTransaction();

                    connection.Execute(
                        "[dbo].[ApiProcesarToma_DOS]",
                        new
                        {
                            data.CodigoBarra,
                            data.NumeroPagina,
                            data.Observacion,
                            data.Cantidad,
                            data.Valor
                        },
                        transaction,
                        commandType: CommandType.StoredProcedure);

                    const string actualizarAutor = @"
UPDATE dbo.TOMAFISICA2
SET autor = @Autor
WHERE NumeroPagina = @NumeroPagina;";

                    var filasActualizadas = connection.Execute(
                        actualizarAutor,
                        new { data.Autor, data.NumeroPagina },
                        transaction);

                    if (filasActualizadas == 0)
                        throw new InvalidOperationException("No se encontro la Toma Fisica 2 para asignar el autor.");

                    transaction.Commit();
                    return 1;
                }
                catch (Exception fail)
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
