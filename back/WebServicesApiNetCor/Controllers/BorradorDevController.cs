using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using WebServicesApiNetCor.Util;

namespace WebServicesApiNetCor.Controllers
{
    [ApiController]
    [Route("borrador-dev")]
    public class BorradorDevController : ControllerBase
    {
        public class ScanLineRequest
        {
            public string CodigoBarra { get; set; }
            public decimal? Cantidad { get; set; }
            public int? NumeroCaja { get; set; }
        }

        public class CreateRequest
        {
            public DateTime? Fecha { get; set; }
            public string Observacion { get; set; }
            public string Usuario { get; set; }
            public int? Status { get; set; }
            public List<ScanLineRequest> Detalles { get; set; }
        }

        public class ScanRequest
        {
            public List<ScanLineRequest> Detalles { get; set; }
        }

        public class RunRequest
        {
            public long? Numero { get; set; }
            public bool Aprobar { get; set; }
        }

        public class UpdateLineRequest
        {
            public string CodigoBarra { get; set; }
            public decimal Cantidad { get; set; }
            public int NumeroCaja { get; set; }
            public int? NumeroCajaOriginal { get; set; }
        }

        public class UpdateDetallesRequest
        {
            public List<UpdateLineRequest> Detalles { get; set; }
        }

        public class DevBorradorListItem
        {
            public long Numero { get; set; }
            public DateTime? Fecha { get; set; }
            public string Observacion { get; set; }
            public string Usuario { get; set; }
            public int? Status { get; set; }
        }

        private static string ConnectionString
        {
            get { return Modulo.connectionString; }
        }

        private static string NormCode(string value)
        {
            return (value ?? string.Empty).Trim();
        }

        private static int NormCaja(int? value)
        {
            if (!value.HasValue || value.Value <= 0)
                return 1;

            return value.Value;
        }

        private static decimal NormCantidad(decimal? value)
        {
            if (!value.HasValue || value.Value <= 0)
                return 1m;

            return value.Value;
        }

        private static SqlCommand CreateCommand(string sql, SqlConnection connection, SqlTransaction transaction = null)
        {
            var command = new SqlCommand(sql, connection);
            if (transaction != null)
                command.Transaction = transaction;

            return command;
        }

        private static async Task<int?> GetStatusAsync(SqlConnection connection, SqlTransaction transaction, long numero)
        {
            using (var command = CreateCommand(
                "SELECT CAST(Status AS INT) FROM dbo.DEVBORRADOR WHERE Numero=@Numero;",
                connection,
                transaction))
            {
                command.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;

                var value = await command.ExecuteScalarAsync();
                if (value == null || value == DBNull.Value)
                    return null;

                return Convert.ToInt32(value);
            }
        }

        private static async Task<bool> CodigoExisteEnInventarioAsync(SqlConnection connection, SqlTransaction transaction, string codigoBarra)
        {
            using (var command = CreateCommand(
                "SELECT 1 FROM dbo.INVENTARIO WHERE CodigoBarra=@CodigoBarra;",
                connection,
                transaction))
            {
                command.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra;
                var exists = await command.ExecuteScalarAsync();
                return exists != null;
            }
        }

        private static async Task<decimal> GetCostoInventarioAsync(SqlConnection connection, SqlTransaction transaction, string codigoBarra)
        {
            try
            {
                using (var command = CreateCommand(@"
SELECT TOP 1 CAST(ISNULL([Costo Inicial], 0) AS DECIMAL(18,8))
FROM dbo.INVENTARIO
WHERE CodigoBarra=@CodigoBarra;", connection, transaction))
                {
                    command.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra;
                    var value = await command.ExecuteScalarAsync();

                    if (value == null || value == DBNull.Value)
                        return 0m;

                    return Convert.ToDecimal(value);
                }
            }
            catch (SqlException ex) when (ex.Number == 207)
            {
            }

            using (var command = CreateCommand(@"
SELECT TOP 1 CAST(ISNULL([CostoInicial], 0) AS DECIMAL(18,8))
FROM dbo.INVENTARIO
WHERE CodigoBarra=@CodigoBarra;", connection, transaction))
            {
                command.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra;
                var value = await command.ExecuteScalarAsync();

                if (value == null || value == DBNull.Value)
                    return 0m;

                return Convert.ToDecimal(value);
            }
        }

        private static async Task<int> GetNextItemAsync(SqlConnection connection, SqlTransaction transaction, long numero)
        {
            using (var command = CreateCommand(@"
SELECT ISNULL(MAX(Item),0) + 1
FROM dbo.MOVDEVBORRADOR WITH (UPDLOCK, HOLDLOCK)
WHERE Numero=@Numero;", connection, transaction))
            {
                command.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                var value = await command.ExecuteScalarAsync();
                return Convert.ToInt32(value);
            }
        }

        private static async Task<long> GetNextNumeroAsync(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = CreateCommand(@"
SELECT ISNULL(MAX(Numero),0) + 1
FROM dbo.DEVBORRADOR WITH (UPDLOCK, HOLDLOCK);", connection, transaction))
            {
                var value = await command.ExecuteScalarAsync();
                return Convert.ToInt64(value);
            }
        }

        private static async Task<Tuple<int, int>> AddOrIncrementAsync(
            SqlConnection connection,
            SqlTransaction transaction,
            long numero,
            string codigoBarra,
            int numeroCaja,
            decimal addCantidad)
        {
            using (var selectCommand = CreateCommand(@"
SELECT TOP 1 Item
FROM dbo.MOVDEVBORRADOR WITH (UPDLOCK, HOLDLOCK)
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@NumeroCaja;", connection, transaction))
            {
                selectCommand.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                selectCommand.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra;
                selectCommand.Parameters.Add("@NumeroCaja", SqlDbType.Int).Value = numeroCaja;

                var existingItem = await selectCommand.ExecuteScalarAsync();

                if (existingItem != null)
                {
                    using (var updateCommand = CreateCommand(@"
UPDATE dbo.MOVDEVBORRADOR
SET Cantidad = Cantidad + @AddCantidad
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@NumeroCaja;", connection, transaction))
                    {
                        updateCommand.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                        updateCommand.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra;
                        updateCommand.Parameters.Add("@NumeroCaja", SqlDbType.Int).Value = numeroCaja;

                        var addParameter = updateCommand.Parameters.Add("@AddCantidad", SqlDbType.Decimal);
                        addParameter.Precision = 18;
                        addParameter.Scale = 2;
                        addParameter.Value = addCantidad;

                        var rows = await updateCommand.ExecuteNonQueryAsync();
                        return Tuple.Create(0, rows > 0 ? 1 : 0);
                    }
                }
            }

            var nextItem = await GetNextItemAsync(connection, transaction, numero);
            var costo = await GetCostoInventarioAsync(connection, transaction, codigoBarra);

            using (var insertCommand = CreateCommand(@"
INSERT INTO dbo.MOVDEVBORRADOR (Numero, CodigoBarra, Cantidad, NumeroCaja, Item, Costo)
VALUES (@Numero, @CodigoBarra, @Cantidad, @NumeroCaja, @Item, @Costo);", connection, transaction))
            {
                insertCommand.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                insertCommand.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra;

                var cantidadParameter = insertCommand.Parameters.Add("@Cantidad", SqlDbType.Decimal);
                cantidadParameter.Precision = 18;
                cantidadParameter.Scale = 2;
                cantidadParameter.Value = addCantidad;

                insertCommand.Parameters.Add("@NumeroCaja", SqlDbType.Int).Value = numeroCaja;
                insertCommand.Parameters.Add("@Item", SqlDbType.Int).Value = nextItem;

                var costoParameter = insertCommand.Parameters.Add("@Costo", SqlDbType.Decimal);
                costoParameter.Precision = 18;
                costoParameter.Scale = 8;
                costoParameter.Value = costo;

                await insertCommand.ExecuteNonQueryAsync();
            }

            return Tuple.Create(1, 0);
        }

        private static object ConflictLocked(long numero)
        {
            return new
            {
                ok = false,
                code = "APPROVED_LOCKED",
                error = "El numero " + numero + " esta aprobado (Status=1) y no se puede editar."
            };
        }

        [HttpGet("ping")]
        public IActionResult Ping()
        {
            return Ok(new
            {
                Ok = true,
                Service = "Borrador Dev",
                ListenerOk = true,
                DatabaseOk = false,
                DatabaseMessage = "Ping del escucha OK. La base de datos no se valido en esta prueba.",
                ServerTime = DateTime.Now
            });
        }

        [HttpGet("health")]
        public async Task<IActionResult> Health()
        {
            var response = new
            {
                Ok = true,
                Service = "Borrador Dev",
                ListenerOk = true,
                DatabaseOk = false,
                DatabaseMessage = "No se pudo validar la base de datos.",
                ServerTime = DateTime.Now
            };

            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                return Ok(new
                {
                    response.Ok,
                    response.Service,
                    response.ListenerOk,
                    response.DatabaseOk,
                    DatabaseMessage = "La cadena de conexion no esta configurada.",
                    response.ServerTime
                });
            }

            try
            {
                var builder = new SqlConnectionStringBuilder(ConnectionString)
                {
                    ConnectTimeout = 8
                };

                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("SELECT 1", connection))
                    {
                        await command.ExecuteScalarAsync();
                    }
                }

                return Ok(new
                {
                    response.Ok,
                    response.Service,
                    response.ListenerOk,
                    DatabaseOk = true,
                    DatabaseMessage = "SQL OK",
                    response.ServerTime
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    response.Ok,
                    response.Service,
                    response.ListenerOk,
                    response.DatabaseOk,
                    DatabaseMessage = ex.Message,
                    response.ServerTime
                });
            }
        }

        [HttpGet("list")]
        public async Task<IActionResult> List([FromQuery] string q = null, [FromQuery] int top = 50, [FromQuery] int? status = null)
        {
            try
            {
                if (top < 1)
                    top = 1;

                if (top > 200)
                    top = 200;

                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(@"
SELECT TOP (@Top)
    Numero,
    Fecha,
    Observacion,
    Usuario,
    CAST(Status AS INT) AS StatusInt
FROM dbo.DEVBORRADOR
WHERE
    (@Status IS NULL OR CAST(Status AS INT) = @Status)
    AND (
        @Q IS NULL OR @Q = ''
        OR CAST(Numero AS NVARCHAR(50)) LIKE '%' + @Q + '%'
        OR ISNULL(Usuario,'') LIKE '%' + @Q + '%'
        OR ISNULL(Observacion,'') LIKE '%' + @Q + '%'
    )
ORDER BY Numero DESC;", connection))
                    {
                        command.Parameters.Add("@Top", SqlDbType.Int).Value = top;
                        command.Parameters.Add("@Q", SqlDbType.NVarChar, 200).Value = (object)q ?? DBNull.Value;
                        command.Parameters.Add("@Status", SqlDbType.Int).Value = (object)status ?? DBNull.Value;

                        var items = new List<DevBorradorListItem>();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                items.Add(new DevBorradorListItem
                                {
                                    Numero = Convert.ToInt64(reader.GetValue(0)),
                                    Fecha = reader.IsDBNull(1) ? (DateTime?)null : (DateTime?)reader.GetValue(1),
                                    Observacion = reader.IsDBNull(2) ? null : Convert.ToString(reader.GetValue(2)),
                                    Usuario = reader.IsDBNull(3) ? null : Convert.ToString(reader.GetValue(3)),
                                    Status = reader.IsDBNull(4) ? (int?)null : Convert.ToInt32(reader.GetValue(4))
                                });
                            }
                        }

                        return Ok(new { ok = true, items = items });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException != null ? ex.InnerException.Message : null });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] CreateRequest request)
        {
            try
            {
                if (request == null || request.Detalles == null || request.Detalles.Count == 0)
                    return BadRequest(new { ok = false, error = "Debe enviar al menos 1 detalle en Detalles." });

                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                    {
                        foreach (var detalle in request.Detalles)
                        {
                            var codigo = NormCode(detalle.CodigoBarra);
                            if (string.IsNullOrWhiteSpace(codigo))
                            {
                                transaction.Rollback();
                                return BadRequest(new { ok = false, error = "CodigoBarra es requerido." });
                            }

                            if (!await CodigoExisteEnInventarioAsync(connection, transaction, codigo))
                            {
                                transaction.Rollback();
                                return BadRequest(new { ok = false, error = "El CodigoBarra '" + codigo + "' no existe en INVENTARIO." });
                            }
                        }

                        var numero = await GetNextNumeroAsync(connection, transaction);

                        var fecha = request.Fecha ?? DateTime.Now;
                        var statusValue = request.Status ?? 0;
                        var observacion = request.Observacion ?? string.Empty;
                        var usuario = request.Usuario ?? "API";

                        using (var insertHeader = CreateCommand(@"
INSERT INTO dbo.DEVBORRADOR (Numero, Fecha, Observacion, Usuario, Status)
VALUES (@Numero, @Fecha, @Observacion, @Usuario, @Status);", connection, transaction))
                        {
                            insertHeader.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                            insertHeader.Parameters.Add("@Fecha", SqlDbType.DateTime).Value = fecha;
                            insertHeader.Parameters.Add("@Observacion", SqlDbType.NVarChar, -1).Value = observacion;
                            insertHeader.Parameters.Add("@Usuario", SqlDbType.NVarChar, -1).Value = usuario;
                            insertHeader.Parameters.Add("@Status", SqlDbType.Int).Value = statusValue;
                            await insertHeader.ExecuteNonQueryAsync();
                        }

                        var inserted = 0;
                        var updated = 0;

                        foreach (var detalle in request.Detalles)
                        {
                            var result = await AddOrIncrementAsync(
                                connection,
                                transaction,
                                numero,
                                NormCode(detalle.CodigoBarra),
                                NormCaja(detalle.NumeroCaja),
                                NormCantidad(detalle.Cantidad));

                            inserted += result.Item1;
                            updated += result.Item2;
                        }

                        transaction.Commit();
                        return Ok(new { ok = true, numero = numero, inserted = inserted, updated = updated, detalles = request.Detalles.Count });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException != null ? ex.InnerException.Message : null });
            }
        }

        [HttpPost("{numero:long}/scan")]
        public async Task<IActionResult> Scan(long numero, [FromBody] ScanRequest request)
        {
            try
            {
                if (request == null || request.Detalles == null || request.Detalles.Count == 0)
                    return BadRequest(new { ok = false, error = "Debe enviar al menos 1 detalle." });

                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    var status = await GetStatusAsync(connection, null, numero);
                    if (!status.HasValue)
                        return NotFound(new { ok = false, error = "El numero " + numero + " no existe." });

                    if (status.Value == 1)
                        return StatusCode(409, ConflictLocked(numero));

                    using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                    {
                        var transactionStatus = await GetStatusAsync(connection, transaction, numero);
                        if (!transactionStatus.HasValue)
                        {
                            transaction.Rollback();
                            return NotFound(new { ok = false, error = "El numero " + numero + " no existe." });
                        }

                        if (transactionStatus.Value == 1)
                        {
                            transaction.Rollback();
                            return StatusCode(409, ConflictLocked(numero));
                        }

                        foreach (var detalle in request.Detalles)
                        {
                            var codigo = NormCode(detalle.CodigoBarra);
                            if (string.IsNullOrWhiteSpace(codigo))
                            {
                                transaction.Rollback();
                                return BadRequest(new { ok = false, error = "CodigoBarra es requerido." });
                            }

                            if (!await CodigoExisteEnInventarioAsync(connection, transaction, codigo))
                            {
                                transaction.Rollback();
                                return BadRequest(new { ok = false, error = "El CodigoBarra '" + codigo + "' no existe en INVENTARIO." });
                            }
                        }

                        var inserted = 0;
                        var updated = 0;

                        foreach (var detalle in request.Detalles)
                        {
                            var result = await AddOrIncrementAsync(
                                connection,
                                transaction,
                                numero,
                                NormCode(detalle.CodigoBarra),
                                NormCaja(detalle.NumeroCaja),
                                NormCantidad(detalle.Cantidad));

                            inserted += result.Item1;
                            updated += result.Item2;
                        }

                        transaction.Commit();
                        return Ok(new { ok = true, numero = numero, inserted = inserted, updated = updated, detalles = request.Detalles.Count });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException != null ? ex.InnerException.Message : null });
            }
        }

        [HttpGet("{numero:long}")]
        public async Task<IActionResult> Get(long numero)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    object header = null;

                    using (var headerCommand = new SqlCommand(@"
SELECT TOP 1 Numero, Fecha, Observacion, Usuario, Status
FROM dbo.DEVBORRADOR
WHERE Numero=@Numero;", connection))
                    {
                        headerCommand.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;

                        using (var reader = await headerCommand.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                header = new
                                {
                                    Numero = reader.IsDBNull(0) ? (long?)null : Convert.ToInt64(reader.GetValue(0)),
                                    Fecha = reader.IsDBNull(1) ? (DateTime?)null : (DateTime?)reader.GetValue(1),
                                    Observacion = reader.IsDBNull(2) ? null : Convert.ToString(reader.GetValue(2)),
                                    Usuario = reader.IsDBNull(3) ? null : Convert.ToString(reader.GetValue(3)),
                                    Status = reader.IsDBNull(4) ? (int?)null : Convert.ToInt32(reader.GetValue(4))
                                };
                            }
                        }
                    }

                    if (header == null)
                        return NotFound(new { ok = false, error = "El numero " + numero + " no existe." });

                    var detalles = new List<object>();

                    using (var detalleCommand = new SqlCommand(@"
SELECT
    m.Numero,
    m.CodigoBarra,
    i.Referencia,
    i.Nombre,
    m.Cantidad,
    m.NumeroCaja,
    m.Item,
    m.Costo
FROM dbo.MOVDEVBORRADOR m
LEFT JOIN dbo.INVENTARIO i
  ON i.CodigoBarra = m.CodigoBarra
WHERE m.Numero=@Numero
ORDER BY m.Item;", connection))
                    {
                        detalleCommand.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;

                        using (var reader = await detalleCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                detalles.Add(new
                                {
                                    Numero = reader.IsDBNull(0) ? (long?)null : Convert.ToInt64(reader.GetValue(0)),
                                    CodigoBarra = reader.IsDBNull(1) ? null : Convert.ToString(reader.GetValue(1)),
                                    Referencia = reader.IsDBNull(2) ? null : Convert.ToString(reader.GetValue(2)),
                                    Nombre = reader.IsDBNull(3) ? null : Convert.ToString(reader.GetValue(3)),
                                    Cantidad = reader.IsDBNull(4) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(4)),
                                    NumeroCaja = reader.IsDBNull(5) ? (int?)null : Convert.ToInt32(reader.GetValue(5)),
                                    Item = reader.IsDBNull(6) ? (int?)null : Convert.ToInt32(reader.GetValue(6)),
                                    Costo = reader.IsDBNull(7) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(7))
                                });
                            }
                        }
                    }

                    return Ok(new { ok = true, header = header, detalles = detalles });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException != null ? ex.InnerException.Message : null });
            }
        }

        [HttpPost("run")]
        public async Task<IActionResult> Run([FromBody] RunRequest request)
        {
            try
            {
                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    object numeroParam = DBNull.Value;
                    if (request != null && request.Numero.HasValue)
                    {
                        if (request.Numero.Value > int.MaxValue || request.Numero.Value < int.MinValue)
                            return BadRequest(new { ok = false, error = "El numero " + request.Numero.Value + " excede el rango INT del procedimiento almacenado." });

                        numeroParam = Convert.ToInt32(request.Numero.Value);
                    }

                    using (var command = new SqlCommand("dbo.Borrador_Dev", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@Numero", SqlDbType.Int) { Value = numeroParam });
                        command.Parameters.Add(new SqlParameter("@MarcarProcesado", SqlDbType.Bit) { Value = request != null && request.Aprobar ? 1 : 0 });

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (!reader.HasRows)
                                return Ok(new { ok = true, info = (object)null });

                            await reader.ReadAsync();

                            var info = new Dictionary<string, object>();
                            for (var i = 0; i < reader.FieldCount; i++)
                                info[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                            return Ok(new { ok = true, info = info });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException != null ? ex.InnerException.Message : null });
            }
        }

        [HttpPut("{numero:long}/detalle")]
        public async Task<IActionResult> UpdateDetalle(long numero, [FromBody] UpdateDetallesRequest request)
        {
            try
            {
                if (request == null || request.Detalles == null || request.Detalles.Count == 0)
                    return BadRequest(new { ok = false, error = "Debe enviar al menos 1 detalle." });

                using (var connection = new SqlConnection(ConnectionString))
                {
                    await connection.OpenAsync();

                    var status = await GetStatusAsync(connection, null, numero);
                    if (!status.HasValue)
                        return NotFound(new { ok = false, error = "El numero " + numero + " no existe." });

                    if (status.Value == 1)
                        return StatusCode(409, ConflictLocked(numero));

                    using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                    {
                        var transactionStatus = await GetStatusAsync(connection, transaction, numero);
                        if (!transactionStatus.HasValue)
                        {
                            transaction.Rollback();
                            return NotFound(new { ok = false, error = "El numero " + numero + " no existe." });
                        }

                        if (transactionStatus.Value == 1)
                        {
                            transaction.Rollback();
                            return StatusCode(409, ConflictLocked(numero));
                        }

                        foreach (var detalle in request.Detalles)
                        {
                            var codigo = NormCode(detalle.CodigoBarra);

                            if (string.IsNullOrWhiteSpace(codigo))
                            {
                                transaction.Rollback();
                                return BadRequest(new { ok = false, error = "CodigoBarra es requerido." });
                            }

                            if (!await CodigoExisteEnInventarioAsync(connection, transaction, codigo))
                            {
                                transaction.Rollback();
                                return BadRequest(new { ok = false, error = "El CodigoBarra '" + codigo + "' no existe en INVENTARIO." });
                            }

                            if (detalle.Cantidad <= 0)
                            {
                                transaction.Rollback();
                                return BadRequest(new { ok = false, error = "Cantidad debe ser > 0." });
                            }

                            if (detalle.NumeroCaja <= 0)
                            {
                                transaction.Rollback();
                                return BadRequest(new { ok = false, error = "NumeroCaja debe ser >= 1." });
                            }
                        }

                        var updated = 0;
                        var inserted = 0;
                        var deleted = 0;

                        foreach (var detalle in request.Detalles)
                        {
                            var codigo = NormCode(detalle.CodigoBarra);
                            var cajaDestino = detalle.NumeroCaja;
                            var cajaOrigen = detalle.NumeroCajaOriginal ?? detalle.NumeroCaja;

                            using (var selectOrigin = CreateCommand(@"
SELECT TOP 1 Item
FROM dbo.MOVDEVBORRADOR WITH (UPDLOCK, HOLDLOCK)
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@CajaOrig;", connection, transaction))
                            {
                                selectOrigin.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                                selectOrigin.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;
                                selectOrigin.Parameters.Add("@CajaOrig", SqlDbType.Int).Value = cajaOrigen;

                                var existsOrigin = await selectOrigin.ExecuteScalarAsync() != null;

                                if (!existsOrigin)
                                {
                                    var nextItem = await GetNextItemAsync(connection, transaction, numero);
                                    var costo = await GetCostoInventarioAsync(connection, transaction, codigo);

                                    using (var insertDetail = CreateCommand(@"
INSERT INTO dbo.MOVDEVBORRADOR (Numero, CodigoBarra, Cantidad, NumeroCaja, Item, Costo)
VALUES (@Numero, @CodigoBarra, @Cantidad, @NumeroCaja, @Item, @Costo);", connection, transaction))
                                    {
                                        insertDetail.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                                        insertDetail.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;

                                        var cantidadParameter = insertDetail.Parameters.Add("@Cantidad", SqlDbType.Decimal);
                                        cantidadParameter.Precision = 18;
                                        cantidadParameter.Scale = 2;
                                        cantidadParameter.Value = detalle.Cantidad;

                                        insertDetail.Parameters.Add("@NumeroCaja", SqlDbType.Int).Value = cajaDestino;
                                        insertDetail.Parameters.Add("@Item", SqlDbType.Int).Value = nextItem;

                                        var costoParameter = insertDetail.Parameters.Add("@Costo", SqlDbType.Decimal);
                                        costoParameter.Precision = 18;
                                        costoParameter.Scale = 8;
                                        costoParameter.Value = costo;

                                        await insertDetail.ExecuteNonQueryAsync();
                                        inserted++;
                                    }

                                    continue;
                                }
                            }

                            if (cajaOrigen == cajaDestino)
                            {
                                using (var updateSameBox = CreateCommand(@"
UPDATE dbo.MOVDEVBORRADOR
SET Cantidad=@Cantidad
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@NumeroCaja;", connection, transaction))
                                {
                                    updateSameBox.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                                    updateSameBox.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;
                                    updateSameBox.Parameters.Add("@NumeroCaja", SqlDbType.Int).Value = cajaDestino;

                                    var cantidadParameter = updateSameBox.Parameters.Add("@Cantidad", SqlDbType.Decimal);
                                    cantidadParameter.Precision = 18;
                                    cantidadParameter.Scale = 2;
                                    cantidadParameter.Value = detalle.Cantidad;

                                    var rows = await updateSameBox.ExecuteNonQueryAsync();
                                    if (rows > 0)
                                        updated++;
                                }

                                continue;
                            }

                            var existsDestino = false;
                            using (var selectDest = CreateCommand(@"
SELECT 1
FROM dbo.MOVDEVBORRADOR WITH (UPDLOCK, HOLDLOCK)
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@CajaDest;", connection, transaction))
                            {
                                selectDest.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                                selectDest.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;
                                selectDest.Parameters.Add("@CajaDest", SqlDbType.Int).Value = cajaDestino;
                                existsDestino = await selectDest.ExecuteScalarAsync() != null;
                            }

                            if (existsDestino)
                            {
                                using (var updateDest = CreateCommand(@"
UPDATE dbo.MOVDEVBORRADOR
SET Cantidad = Cantidad + @AddCantidad
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@CajaDest;", connection, transaction))
                                {
                                    updateDest.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                                    updateDest.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;
                                    updateDest.Parameters.Add("@CajaDest", SqlDbType.Int).Value = cajaDestino;

                                    var addParameter = updateDest.Parameters.Add("@AddCantidad", SqlDbType.Decimal);
                                    addParameter.Precision = 18;
                                    addParameter.Scale = 2;
                                    addParameter.Value = detalle.Cantidad;

                                    var rows = await updateDest.ExecuteNonQueryAsync();
                                    if (rows > 0)
                                        updated++;
                                }

                                using (var deleteOrigin = CreateCommand(@"
DELETE FROM dbo.MOVDEVBORRADOR
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@CajaOrig;", connection, transaction))
                                {
                                    deleteOrigin.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                                    deleteOrigin.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;
                                    deleteOrigin.Parameters.Add("@CajaOrig", SqlDbType.Int).Value = cajaOrigen;

                                    var rows = await deleteOrigin.ExecuteNonQueryAsync();
                                    if (rows > 0)
                                        deleted++;
                                }

                                continue;
                            }

                            using (var moveUpdate = CreateCommand(@"
UPDATE dbo.MOVDEVBORRADOR
SET NumeroCaja=@CajaDest, Cantidad=@Cantidad
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@CajaOrig;", connection, transaction))
                            {
                                moveUpdate.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                                moveUpdate.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;
                                moveUpdate.Parameters.Add("@CajaOrig", SqlDbType.Int).Value = cajaOrigen;
                                moveUpdate.Parameters.Add("@CajaDest", SqlDbType.Int).Value = cajaDestino;

                                var cantidadParameter = moveUpdate.Parameters.Add("@Cantidad", SqlDbType.Decimal);
                                cantidadParameter.Precision = 18;
                                cantidadParameter.Scale = 2;
                                cantidadParameter.Value = detalle.Cantidad;

                                var rows = await moveUpdate.ExecuteNonQueryAsync();
                                if (rows > 0)
                                    updated++;
                            }
                        }

                        transaction.Commit();
                        return Ok(new { ok = true, numero = numero, updated = updated, inserted = inserted, deleted = deleted });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException != null ? ex.InnerException.Message : null });
            }
        }
    }
}
