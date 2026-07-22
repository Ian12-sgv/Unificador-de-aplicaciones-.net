using System.Data.SqlClient;
using System.Data;
using WebServicesApiNetCor.Util;
using Dapper;
using log4net;
using System;
using WebServicesApiNetCor.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebServicesApiNetCor.Data
{
    public class TransferenciaRepository
    {

        private string connectionString;

        public TransferenciaRepository()
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

        public async Task<IEnumerable<TransferenciaHistorial>> GetHistorial()
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try
                {
                    const string sql = @"
                        SELECT
                            T.Numero,
                            T.Fecha,
                            T.CodigoEnvia,
                            T.CodigoRecibe,
                            T.DocumentoOrigen,
                            T.TipoValor,
                            T.TotalValor,
                            T.Observacion,
                            T.Status,
                            T.Usuario,
                            T.InterContable,
                            T.FechaEmision,
                            T.IdLote,
                            T.IdDespacho,
                            T.Correccion,
                            COALESCE(SR.Nombre, SE.Nombre, T.CodigoRecibe, T.CodigoEnvia, '') AS SucursalNombre,
                            COUNT(DISTINCT M.CodigoBarra) AS Referencias,
                            ISNULL(SUM(M.Cantidad), 0) AS Unidades
                        FROM Transferencias T
                        LEFT JOIN movTransferencias M ON M.Numero = T.Numero
                        LEFT JOIN sucursales SR ON SR.Codigo = T.CodigoRecibe
                        LEFT JOIN sucursales SE ON SE.Codigo = T.CodigoEnvia
                        GROUP BY
                            T.Numero,
                            T.Fecha,
                            T.CodigoEnvia,
                            T.CodigoRecibe,
                            T.DocumentoOrigen,
                            T.TipoValor,
                            T.TotalValor,
                            T.Observacion,
                            T.Status,
                            T.Usuario,
                            T.InterContable,
                            T.FechaEmision,
                            T.IdLote,
                            T.IdDespacho,
                            T.Correccion,
                            SR.Nombre,
                            SE.Nombre
                        ORDER BY T.Fecha DESC, T.Numero DESC;";
                    dbConnection.Open();
                    return await dbConnection.QueryAsync<TransferenciaHistorial>(sql);
                }
                catch (Exception fail)
                {
                    log.Error(fail.Message);
                    return new List<TransferenciaHistorial>();
                }
            }
        }

        public async Task<IEnumerable<TransferenciaHistorial>> GetHistorialPorEstado(bool status)
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try
                {
                    const string sql = @"
                        SELECT
                            T.Numero,
                            T.Fecha,
                            T.CodigoEnvia,
                            T.CodigoRecibe,
                            T.DocumentoOrigen,
                            T.TipoValor,
                            T.TotalValor,
                            T.Observacion,
                            T.Status,
                            T.Usuario,
                            T.InterContable,
                            T.FechaEmision,
                            T.IdLote,
                            T.IdDespacho,
                            T.Correccion,
                            COALESCE(SR.Nombre, SE.Nombre, T.CodigoRecibe, T.CodigoEnvia, '') AS SucursalNombre,
                            COUNT(DISTINCT M.CodigoBarra) AS Referencias,
                            ISNULL(SUM(M.Cantidad), 0) AS Unidades
                        FROM Transferencias T
                        LEFT JOIN movTransferencias M ON M.Numero = T.Numero
                        LEFT JOIN sucursales SR ON SR.Codigo = T.CodigoRecibe
                        LEFT JOIN sucursales SE ON SE.Codigo = T.CodigoEnvia
                        WHERE T.Status = @Status
                        GROUP BY
                            T.Numero,
                            T.Fecha,
                            T.CodigoEnvia,
                            T.CodigoRecibe,
                            T.DocumentoOrigen,
                            T.TipoValor,
                            T.TotalValor,
                            T.Observacion,
                            T.Status,
                            T.Usuario,
                            T.InterContable,
                            T.FechaEmision,
                            T.IdLote,
                            T.IdDespacho,
                            T.Correccion,
                            SR.Nombre,
                            SE.Nombre
                        ORDER BY T.Fecha DESC, T.Numero DESC;";
                    dbConnection.Open();
                    return await dbConnection.QueryAsync<TransferenciaHistorial>(sql, new { Status = status });
                }
                catch (Exception fail)
                {
                    log.Error(fail.Message);
                    return new List<TransferenciaHistorial>();
                }
            }
        }

        public async Task<IEnumerable<Transferencia>> Get(string Numero)
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try
                {
                    const string sql = @"SELECT * FROM Transferencias WHERE Numero = @Numero;";
                    dbConnection.Open();
                    return await dbConnection.QueryAsync<Transferencia>(sql, new { Numero });
                }
                catch (Exception fail)
                {
                    log.Error(fail.Message);
                    return new List<Transferencia>();
                }
            }
        }

        public int Create(TransferenciaRegistro data)
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try
                {
                    dbConnection.Open();
                    using (var transaction = dbConnection.BeginTransaction())
                    {
                        try
                        {
                            var detalles = (data.Detalles ?? new List<MoviTransferencia>()).ToList();
                            if (detalles.Count == 0)
                            {
                                throw new Exception("La transferencia no tiene detalle.");
                            }

                            var nuevoNumero = dbConnection.QuerySingle<int>(@"
                                SELECT ISNULL(MAX(CAST(Numero AS INT)), 0) + 1
                                FROM TRANSFERENCIAS WITH (UPDLOCK, HOLDLOCK);", transaction: transaction);

                            var siguienteCaja = dbConnection.QuerySingle<int>(@"
                                SELECT ISNULL(MAX(NumeroCaja), 0) + 1
                                FROM movTRANSFERENCIAS WITH (UPDLOCK, HOLDLOCK);", transaction: transaction);

                            var fecha = DateTime.Now;
                            var totalValor = detalles.Sum(x => x.Cantidad * x.Valor);
                            var numeroFormateado = nuevoNumero.ToString();

                            const string headerSql = @"
                                INSERT INTO Transferencias
                                    (Numero, Fecha, CodigoRecibe, CodigoEnvia, DocumentoOrigen, TotalValor, Observacion, Status, Usuario, FechaEmision, Correccion)
                                VALUES
                                    (@Numero, @Fecha, @CodigoRecibe, @CodigoEnvia, @DocumentoOrigen, @TotalValor, @Observacion, @Status, @Usuario, @FechaEmision, @Correccion);";

                            dbConnection.Execute(headerSql, new
                            {
                                Numero = numeroFormateado,
                                Fecha = fecha,
                                data.CodigoRecibe,
                                data.CodigoEnvia,
                                DocumentoOrigen = data.DocumentoOrigen ?? string.Empty,
                                TotalValor = totalValor,
                                Observacion = data.Observacion ?? string.Empty,
                                data.Status,
                                Usuario = data.Usuario ?? string.Empty,
                                FechaEmision = fecha,
                                data.Correccion
                            }, transaction);

                            const string detailSql = @"
                                INSERT INTO movTransferencias
                                    (Numero, Fecha, CodigoBarra, Cantidad, Valor, NumeroCaja, Item)
                                VALUES
                                    (@Numero, @Fecha, @CodigoBarra, @Cantidad, @Valor, @NumeroCaja, @Item);";

                            var item = 1;
                            foreach (var detalle in detalles)
                            {
                                dbConnection.Execute(detailSql, new
                                {
                                    Numero = numeroFormateado,
                                    Fecha = fecha,
                                    detalle.CodigoBarra,
                                    detalle.Cantidad,
                                    detalle.Valor,
                                    NumeroCaja = siguienteCaja,
                                    Item = item++
                                }, transaction);
                            }

                            transaction.Commit();
                            return nuevoNumero;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
                catch (Exception fail)
                {
                    log.Error(fail.Message);
                    return -1;
                }
            }
        }

        public int CreateDetalle(MoviTransferencia data)
        {
            using (IDbConnection dbConnection = Connection)
            {
                var log = LogManager.GetLogger(Startup.repository.Name, typeof(Startup));
                try
                {
                    dbConnection.Open();
                    var dbparams = new DynamicParameters();
                    dbparams.Add("@Numero", data.Numero);
                    dbparams.Add("@Fecha", data.Fecha);
                    dbparams.Add("@CodigoBarra", data.CodigoBarra);
                    dbparams.Add("@Cantidad", data.Cantidad);
                    dbparams.Add("@Valor", data.Valor);
                    dbparams.Add("@NumeroCaja", data.NumeroCaja);
                    dbparams.Add("@Item", data.Item);

                    const string sql = @"INSERT INTO movTransferencias
                        (Numero, Fecha, CodigoBarra, Cantidad, Valor, NumeroCaja, Item)
                        VALUES
                        (@Numero, @Fecha, @CodigoBarra, @Cantidad, @Valor, @NumeroCaja, @Item)";

                    return dbConnection.Execute(sql, dbparams);
                }
                catch (Exception fail)
                {
                    log.Error(fail.Message);
                    return -1;
                }
            }
        }
    }
}
