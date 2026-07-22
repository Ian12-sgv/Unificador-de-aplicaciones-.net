using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BackBorradorDev.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TransferenciaController : ControllerBase
{
    private readonly IConfiguration _config;

    public TransferenciaController(IConfiguration config)
    {
        _config = config;
    }

    public record TransferenciaDetalleRequest(
        string CodigoBarra,
        decimal Cantidad,
        decimal Valor,
        int? NumeroCaja,
        int? Item
    );

    public record TransferenciaRequest(
        DateTime? Fecha,
        string? CodigoRecibe,
        string? CodigoEnvia,
        string? DocumentoOrigen,
        decimal? TotalValor,
        string? Observacion,
        int? Status,
        string? Usuario,
        DateTime? FechaEmision,
        int? IdLote,
        int? IdDespacho,
        bool Correccion,
        List<TransferenciaDetalleRequest>? Detalles
    );

    public record TransferenciaDespachoItem(
        int IdDespacho,
        string Descripcion
    );

    public record TransferenciaLoteItem(
        int IdLote,
        string Lote
    );

    public record TransferenciaHistorialItem(
        string Numero,
        DateTime Fecha,
        string? CodigoRecibe,
        string? CodigoEnvia,
        string? Observacion,
        int Status,
        string? Usuario
    );

    public record TransferenciaResumenEstados(
        int Guardadas,
        int Aprobadas,
        int Exportadas
    );

    [HttpGet]
    public async Task<ActionResult<List<TransferenciaHistorialItem>>> GetHistorial()
    {
        try
        {
            await using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var items = await QueryHistorialAsync(conn, null);
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [HttpGet("resumen-estados")]
    public async Task<ActionResult<TransferenciaResumenEstados>> GetResumenEstados()
    {
        try
        {
            await using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(@"
SELECT Status, COUNT(*) AS Cantidad
FROM dbo.TRANSFERENCIAS
WHERE Status IN (0, 1, 2)
GROUP BY Status;", conn);
            cmd.CommandTimeout = 3;

            var guardadas = 0;
            var aprobadas = 0;
            var exportadas = 0;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var status = reader["Status"] is DBNull ? -1 : Convert.ToInt32(reader["Status"]);
                var cantidad = reader["Cantidad"] is DBNull ? 0 : Convert.ToInt32(reader["Cantidad"]);

                if (status == 0)
                    guardadas = cantidad;
                else if (status == 1)
                    aprobadas = cantidad;
                else if (status == 2)
                    exportadas = cantidad;
            }

            return Ok(new TransferenciaResumenEstados(guardadas, aprobadas, exportadas));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [HttpGet("estado/{status}")]
    public async Task<ActionResult<List<TransferenciaHistorialItem>>> GetHistorialPorEstado(int status)
    {
        try
        {
            await using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();

            var items = await QueryHistorialAsync(conn, status);
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [HttpGet("despachos")]
    public async Task<ActionResult<List<TransferenciaDespachoItem>>> GetDespachos()
    {
        try
        {
            var items = new List<TransferenciaDespachoItem>();
            await using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(@"
SELECT
    CAST(ID AS INT) AS IdDespacho,
    CAST(Descripcion AS VARCHAR(250)) AS Descripcion
FROM dbo.TIPO_DESPACHO
ORDER BY Descripcion;", conn);
            cmd.CommandTimeout = 3;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new TransferenciaDespachoItem(
                    reader["IdDespacho"] is DBNull ? 0 : Convert.ToInt32(reader["IdDespacho"]),
                    Convert.ToString(reader["Descripcion"]) ?? string.Empty
                ));
            }

            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [HttpGet("lotes")]
    public async Task<ActionResult<List<TransferenciaLoteItem>>> GetLotes()
    {
        try
        {
            var items = new List<TransferenciaLoteItem>();
            await using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(@"
SELECT
    CAST(ID AS INT) AS IdLote,
    CAST(Lote AS VARCHAR(250)) AS Lote
FROM dbo.LOTES
ORDER BY Lote;", conn);
            cmd.CommandTimeout = 3;

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new TransferenciaLoteItem(
                    reader["IdLote"] is DBNull ? 0 : Convert.ToInt32(reader["IdLote"]),
                    Convert.ToString(reader["Lote"]) ?? string.Empty
                ));
            }

            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [HttpGet("{numero}")]
    public async Task<ActionResult<object>> GetPorNumero(string numero)
    {
        try
        {
            await using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();

            await using var headerCmd = new SqlCommand(@"
SELECT TOP 1
    Numero, Fecha, CodigoRecibe, CodigoEnvia, DocumentoOrigen,
    TotalValor, Observacion, Status, Usuario, FechaEmision, IdLote, IdDespacho, Correccion
FROM dbo.Transferencias
WHERE Numero=@Numero;", conn);
            headerCmd.Parameters.Add("@Numero", SqlDbType.VarChar, 20).Value = numero;

            await using var reader = await headerCmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return NotFound(new { ok = false, error = "Transferencia no encontrada." });

            var header = new
            {
                Numero = Convert.ToString(reader["Numero"]) ?? "",
                Fecha = Convert.ToDateTime(reader["Fecha"]),
                CodigoRecibe = reader["CodigoRecibe"] as string,
                CodigoEnvia = reader["CodigoEnvia"] as string,
                DocumentoOrigen = reader["DocumentoOrigen"] as string,
                TotalValor = Convert.ToDecimal(reader["TotalValor"]),
                Observacion = reader["Observacion"] as string,
                Status = reader["Status"] is DBNull ? 0 : Convert.ToInt32(reader["Status"]),
                Usuario = reader["Usuario"] as string,
                FechaEmision = reader["FechaEmision"] is DBNull ? (DateTime?)null : Convert.ToDateTime(reader["FechaEmision"]),
                IdLote = reader["IdLote"] is DBNull ? 0 : Convert.ToInt32(reader["IdLote"]),
                IdDespacho = reader["IdDespacho"] is DBNull ? 0 : Convert.ToInt32(reader["IdDespacho"]),
                Correccion = !(reader["Correccion"] is DBNull) && Convert.ToInt32(reader["Correccion"]) != 0
            };
            await reader.CloseAsync();

            var detalles = new List<object>();
            await using var detailCmd = new SqlCommand(@"
SELECT Numero, Fecha, CodigoBarra, Cantidad, Valor, NumeroCaja, Item
FROM dbo.movTransferencias
WHERE Numero=@Numero
ORDER BY Item;", conn);
            detailCmd.Parameters.Add("@Numero", SqlDbType.VarChar, 20).Value = numero;

            await using var detailReader = await detailCmd.ExecuteReaderAsync();
            while (await detailReader.ReadAsync())
            {
                detalles.Add(new
                {
                    Numero = Convert.ToString(detailReader["Numero"]) ?? "",
                    Fecha = Convert.ToDateTime(detailReader["Fecha"]),
                    CodigoBarra = detailReader["CodigoBarra"] as string,
                    Cantidad = Convert.ToDecimal(detailReader["Cantidad"]),
                    Valor = Convert.ToDecimal(detailReader["Valor"]),
                    NumeroCaja = Convert.ToInt32(detailReader["NumeroCaja"]),
                    Item = Convert.ToInt32(detailReader["Item"])
                });
            }

            return Ok(new { Header = header, Detalles = detalles });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<object>> Crear(TransferenciaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CodigoRecibe))
            return BadRequest(new { ok = false, error = "CodigoRecibe es requerido." });

        if (string.IsNullOrWhiteSpace(request.CodigoEnvia))
            return BadRequest(new { ok = false, error = "CodigoEnvia es requerido." });

        var detalles = request.Detalles?
            .Where(x => !string.IsNullOrWhiteSpace(x.CodigoBarra))
            .ToList() ?? new List<TransferenciaDetalleRequest>();

        if (detalles.Count == 0)
            return BadRequest(new { ok = false, error = "Debe enviar al menos un articulo." });

        try
        {
            await using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                var fecha = DateTime.Now;
                var totalValor = detalles.Sum(x => x.Cantidad * x.Valor);
                var usuario = NormalizarUsuario(request.Usuario);

                var nuevoNumero = await ExecuteScalarIntAsync(conn, tx, @"
SELECT ISNULL(MAX(CAST(Numero AS INT)), 0) + 1
FROM dbo.TRANSFERENCIAS WITH (UPDLOCK, HOLDLOCK);");
                var numeroFormateado = nuevoNumero.ToString();

                var siguienteCaja = await ExecuteScalarIntAsync(conn, tx, @"
SELECT ISNULL(MAX(NumeroCaja), 0) + 1
FROM dbo.movTRANSFERENCIAS WITH (UPDLOCK, HOLDLOCK);");

                await using var headerCmd = new SqlCommand(@"
INSERT INTO dbo.Transferencias
    (Numero, Fecha, CodigoRecibe, CodigoEnvia, DocumentoOrigen,
     TotalValor, Observacion, Status, Usuario, FechaEmision, IdLote, IdDespacho, Correccion)
VALUES
    (@Numero, @Fecha, @CodigoRecibe, @CodigoEnvia, @DocumentoOrigen,
     @TotalValor, @Observacion, @Status, @Usuario, @FechaEmision, @IdLote, @IdDespacho, @Correccion);", conn, tx);

                headerCmd.Parameters.Add("@Numero", SqlDbType.VarChar, 20).Value = numeroFormateado;
                headerCmd.Parameters.Add("@Fecha", SqlDbType.DateTime).Value = fecha;
                headerCmd.Parameters.Add("@CodigoRecibe", SqlDbType.VarChar, 20).Value = request.CodigoRecibe.Trim();
                headerCmd.Parameters.Add("@CodigoEnvia", SqlDbType.VarChar, 20).Value = request.CodigoEnvia.Trim();
                headerCmd.Parameters.Add("@DocumentoOrigen", SqlDbType.VarChar, 20).Value = (object?)request.DocumentoOrigen ?? DBNull.Value;
                AddDecimal(headerCmd, "@TotalValor", totalValor);
                headerCmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 250).Value = (object?)request.Observacion ?? DBNull.Value;
                headerCmd.Parameters.Add("@Status", SqlDbType.Int).Value = NormalizarStatus(request.Status);
                headerCmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 50).Value = usuario;
                headerCmd.Parameters.Add("@FechaEmision", SqlDbType.DateTime).Value = fecha;
                headerCmd.Parameters.Add("@IdLote", SqlDbType.Int).Value = request.IdLote.GetValueOrDefault();
                headerCmd.Parameters.Add("@IdDespacho", SqlDbType.Int).Value = request.IdDespacho.GetValueOrDefault();
                headerCmd.Parameters.Add("@Correccion", SqlDbType.Int).Value = request.Correccion ? 1 : 0;
                await headerCmd.ExecuteNonQueryAsync();

                var item = 1;
                foreach (var detalle in detalles)
                {
                    await using var detailCmd = new SqlCommand(@"
INSERT INTO dbo.movTransferencias
    (Numero, Fecha, CodigoBarra, Cantidad, Valor, NumeroCaja, Item)
VALUES
    (@Numero, @Fecha, @CodigoBarra, @Cantidad, @Valor, @NumeroCaja, @Item);", conn, tx);

                    detailCmd.Parameters.Add("@Numero", SqlDbType.VarChar, 20).Value = numeroFormateado;
                    detailCmd.Parameters.Add("@Fecha", SqlDbType.DateTime).Value = fecha;
                    detailCmd.Parameters.Add("@CodigoBarra", SqlDbType.VarChar, 20).Value = detalle.CodigoBarra.Trim();
                    AddDecimal(detailCmd, "@Cantidad", detalle.Cantidad);
                    AddDecimal(detailCmd, "@Valor", detalle.Valor);
                    detailCmd.Parameters.Add("@NumeroCaja", SqlDbType.Int).Value = siguienteCaja;
                    detailCmd.Parameters.Add("@Item", SqlDbType.Int).Value = item++;
                    await detailCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                return Ok(new
                {
                    ok = true,
                    Numero = numeroFormateado,
                    NumeroCaja = siguienteCaja,
                    Items = detalles.Count,
                    TotalValor = totalValor
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    [HttpPut("{numero}")]
    public async Task<ActionResult<object>> ActualizarGuardada(string numero, TransferenciaRequest request)
    {
        if (string.IsNullOrWhiteSpace(numero))
            return BadRequest(new { ok = false, error = "Numero es requerido." });

        if (string.IsNullOrWhiteSpace(request.CodigoRecibe))
            return BadRequest(new { ok = false, error = "CodigoRecibe es requerido." });

        if (string.IsNullOrWhiteSpace(request.CodigoEnvia))
            return BadRequest(new { ok = false, error = "CodigoEnvia es requerido." });

        var detalles = request.Detalles?
            .Where(x => !string.IsNullOrWhiteSpace(x.CodigoBarra))
            .ToList() ?? new List<TransferenciaDetalleRequest>();

        if (detalles.Count == 0)
            return BadRequest(new { ok = false, error = "Debe enviar al menos un articulo." });

        try
        {
            await using var conn = new SqlConnection(GetConnectionString());
            await conn.OpenAsync();
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync();

            try
            {
                var statusActual = await ExecuteScalarNullableIntAsync(conn, tx, @"
SELECT TOP 1 Status
FROM dbo.TRANSFERENCIAS WITH (UPDLOCK, HOLDLOCK)
WHERE Numero = @Numero;", ("@Numero", numero.Trim()));

                if (!statusActual.HasValue)
                {
                    await tx.RollbackAsync();
                    return NotFound(new { ok = false, error = "Transferencia no encontrada." });
                }

                if (statusActual.Value != 0)
                {
                    await tx.RollbackAsync();
                    return BadRequest(new { ok = false, error = "Solo se pueden modificar transferencias guardadas." });
                }

                var fecha = request.Fecha ?? DateTime.Now;
                var fechaEmision = request.FechaEmision ?? fecha;
                var totalValor = detalles.Sum(x => x.Cantidad * x.Valor);
                var usuario = NormalizarUsuario(request.Usuario);

                await using var headerCmd = new SqlCommand(@"
UPDATE dbo.Transferencias
SET Fecha = @Fecha,
    CodigoRecibe = @CodigoRecibe,
    CodigoEnvia = @CodigoEnvia,
    DocumentoOrigen = @DocumentoOrigen,
    TotalValor = @TotalValor,
    Observacion = @Observacion,
    Status = 0,
    Usuario = @Usuario,
    FechaEmision = @FechaEmision,
    IdLote = @IdLote,
    IdDespacho = @IdDespacho,
    Correccion = @Correccion
WHERE Numero = @Numero;", conn, tx);

                headerCmd.Parameters.Add("@Numero", SqlDbType.VarChar, 20).Value = numero.Trim();
                headerCmd.Parameters.Add("@Fecha", SqlDbType.DateTime).Value = fecha;
                headerCmd.Parameters.Add("@CodigoRecibe", SqlDbType.VarChar, 20).Value = request.CodigoRecibe.Trim();
                headerCmd.Parameters.Add("@CodigoEnvia", SqlDbType.VarChar, 20).Value = request.CodigoEnvia.Trim();
                headerCmd.Parameters.Add("@DocumentoOrigen", SqlDbType.VarChar, 20).Value = (object?)request.DocumentoOrigen ?? DBNull.Value;
                AddDecimal(headerCmd, "@TotalValor", totalValor);
                headerCmd.Parameters.Add("@Observacion", SqlDbType.VarChar, 250).Value = (object?)request.Observacion ?? DBNull.Value;
                headerCmd.Parameters.Add("@Usuario", SqlDbType.VarChar, 50).Value = usuario;
                headerCmd.Parameters.Add("@FechaEmision", SqlDbType.DateTime).Value = fechaEmision;
                headerCmd.Parameters.Add("@IdLote", SqlDbType.Int).Value = request.IdLote.GetValueOrDefault();
                headerCmd.Parameters.Add("@IdDespacho", SqlDbType.Int).Value = request.IdDespacho.GetValueOrDefault();
                headerCmd.Parameters.Add("@Correccion", SqlDbType.Int).Value = request.Correccion ? 1 : 0;
                await headerCmd.ExecuteNonQueryAsync();

                await using (var deleteCmd = new SqlCommand(@"
DELETE FROM dbo.movTransferencias
WHERE Numero = @Numero;", conn, tx))
                {
                    deleteCmd.Parameters.Add("@Numero", SqlDbType.VarChar, 20).Value = numero.Trim();
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                var item = 1;
                foreach (var detalle in detalles)
                {
                    await using var detailCmd = new SqlCommand(@"
INSERT INTO dbo.movTransferencias
    (Numero, Fecha, CodigoBarra, Cantidad, Valor, NumeroCaja, Item)
VALUES
    (@Numero, @Fecha, @CodigoBarra, @Cantidad, @Valor, @NumeroCaja, @Item);", conn, tx);

                    detailCmd.Parameters.Add("@Numero", SqlDbType.VarChar, 20).Value = numero.Trim();
                    detailCmd.Parameters.Add("@Fecha", SqlDbType.DateTime).Value = fecha;
                    detailCmd.Parameters.Add("@CodigoBarra", SqlDbType.VarChar, 20).Value = detalle.CodigoBarra.Trim();
                    AddDecimal(detailCmd, "@Cantidad", detalle.Cantidad);
                    AddDecimal(detailCmd, "@Valor", detalle.Valor);
                    detailCmd.Parameters.Add("@NumeroCaja", SqlDbType.Int).Value = detalle.NumeroCaja.GetValueOrDefault(1);
                    detailCmd.Parameters.Add("@Item", SqlDbType.Int).Value = item++;
                    await detailCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                return Ok(new
                {
                    ok = true,
                    Numero = numero.Trim(),
                    Items = detalles.Count,
                    TotalValor = totalValor
                });
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    private string GetConnectionString()
    {
        var cs = _config.GetConnectionString("SqlServer");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("No esta configurado ConnectionStrings:SqlServer.");

        var builder = new SqlConnectionStringBuilder(cs);
        if (builder.ConnectTimeout > 3)
            builder.ConnectTimeout = 3;

        return builder.ConnectionString;
    }

    private static async Task<int> ExecuteScalarIntAsync(SqlConnection conn, SqlTransaction tx, string sql)
    {
        await using var cmd = new SqlCommand(sql, conn, tx);
        var value = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(value);
    }

    private static async Task<int?> ExecuteScalarNullableIntAsync(SqlConnection conn, SqlTransaction tx, string sql, params (string Name, object Value)[] parameters)
    {
        await using var cmd = new SqlCommand(sql, conn, tx);
        foreach (var parameter in parameters)
            cmd.Parameters.AddWithValue(parameter.Name, parameter.Value);

        var value = await cmd.ExecuteScalarAsync();
        return value is null || value is DBNull ? null : Convert.ToInt32(value);
    }

    private static void AddDecimal(SqlCommand cmd, string name, decimal value)
    {
        var parameter = cmd.Parameters.Add(name, SqlDbType.Decimal);
        parameter.Precision = 18;
        parameter.Scale = 2;
        parameter.Value = value;
    }

    private static string NormalizarUsuario(string? usuario)
    {
        var value = (usuario ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(value))
            return "admi";

        if (string.Equals(value, "admin", StringComparison.OrdinalIgnoreCase))
            return "admi";

        return value;
    }

    private static int NormalizarStatus(int? status)
    {
        var value = status.GetValueOrDefault(0);
        return value is >= 0 and <= 2 ? value : 0;
    }

    private static async Task<List<TransferenciaHistorialItem>> QueryHistorialAsync(SqlConnection conn, int? status)
    {
        var items = new List<TransferenciaHistorialItem>();
        var sql = @"
SELECT TOP (1000)
    T.Numero,
    T.Fecha,
    T.CodigoRecibe,
    T.CodigoEnvia,
    T.Observacion,
    T.Status,
    T.Usuario
FROM dbo.TRANSFERENCIAS T
WHERE (@FiltrarStatus = 0 OR T.Status = @Status)
ORDER BY T.Fecha DESC, T.Numero DESC;";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@FiltrarStatus", SqlDbType.Int).Value = status.HasValue ? 1 : 0;
        cmd.Parameters.Add("@Status", SqlDbType.Int).Value = status.GetValueOrDefault();

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(new TransferenciaHistorialItem(
                Convert.ToString(reader["Numero"]) ?? "",
                Convert.ToDateTime(reader["Fecha"]),
                reader["CodigoRecibe"] as string,
                reader["CodigoEnvia"] as string,
                reader["Observacion"] as string,
                reader["Status"] is DBNull ? 0 : Convert.ToInt32(reader["Status"]),
                reader["Usuario"] as string
            ));
        }

        return items;
    }
}
