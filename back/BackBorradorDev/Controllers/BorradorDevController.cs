using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BackBorradorDev.Controllers;

[ApiController]
[Route("borrador-dev")]
public class BorradorDevController : ControllerBase
{
    private readonly IConfiguration _config;

    public BorradorDevController(IConfiguration config)
    {
        _config = config;
    }

    // =============== Helpers ===============

    private static string NormCode(string s) => (s ?? "").Trim();

    private static int NormCaja(int? n)
    {
        if (!n.HasValue || n.Value <= 0) return 1;
        return n.Value;
    }

    private static decimal NormCantidad(decimal? c)
    {
        if (!c.HasValue || c.Value <= 0) return 1m;
        return c.Value;
    }

    private static async Task<int?> GetStatusAsync(SqlConnection conn, SqlTransaction? tx, long numero)
    {
        await using var cmd = new SqlCommand(
            "SELECT CAST(Status AS INT) FROM dbo.DEVBORRADOR WHERE Numero=@Numero;",
            conn, tx);

        cmd.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;

        var v = await cmd.ExecuteScalarAsync();
        if (v == null) return null;
        return Convert.ToInt32(v);
    }

    private static async Task<bool> CodigoExisteEnInventarioAsync(SqlConnection conn, SqlTransaction? tx, string codigoBarra)
    {
        await using var cmd = new SqlCommand(
            "SELECT 1 FROM dbo.INVENTARIO WHERE CodigoBarra=@CodigoBarra;",
            conn, tx);

        cmd.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra;
        var exists = await cmd.ExecuteScalarAsync();
        return exists != null;
    }

    /// <summary>
    /// Obtiene costo inicial desde INVENTARIO.
    /// Soporta columna "Costo Inicial" (con espacio) o "CostoInicial" (sin espacio).
    /// </summary>
    private static async Task<decimal> GetCostoInventarioAsync(SqlConnection conn, SqlTransaction? tx, string codigoBarra)
    {
        // 1) Intento con columna con espacio: [Costo Inicial]
        try
        {
            await using var cmd = new SqlCommand(@"
SELECT TOP 1 CAST(ISNULL([Costo Inicial], 0) AS DECIMAL(18,8))
FROM dbo.INVENTARIO
WHERE CodigoBarra=@CodigoBarra;", conn, tx);

            cmd.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra;

            var v = await cmd.ExecuteScalarAsync();
            if (v == null || v is DBNull) return 0m;
            return Convert.ToDecimal(v);
        }
        catch (SqlException ex) when (ex.Number == 207) // Invalid column name
        {
            // 2) Fallback a columna sin espacio: [CostoInicial]
        }

        await using (var cmd2 = new SqlCommand(@"
SELECT TOP 1 CAST(ISNULL([CostoInicial], 0) AS DECIMAL(18,8))
FROM dbo.INVENTARIO
WHERE CodigoBarra=@CodigoBarra;", conn, tx))
        {
            cmd2.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra;

            var v2 = await cmd2.ExecuteScalarAsync();
            if (v2 == null || v2 is DBNull) return 0m;
            return Convert.ToDecimal(v2);
        }
    }

    private static async Task<int> GetNextItemAsync(SqlConnection conn, SqlTransaction tx, long numero)
    {
        // UPDLOCK/HOLDLOCK para evitar duplicados de Item en concurrencia
        await using var cmd = new SqlCommand(@"
SELECT ISNULL(MAX(Item),0) + 1
FROM dbo.MOVDEVBORRADOR WITH (UPDLOCK, HOLDLOCK)
WHERE Numero=@Numero;", conn, tx);

        cmd.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;

        var v = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(v);
    }

    private static async Task<long> GetNextNumeroAsync(SqlConnection conn, SqlTransaction tx)
    {
        await using var cmd = new SqlCommand(@"
SELECT ISNULL(MAX(Numero),0) + 1
FROM dbo.DEVBORRADOR WITH (UPDLOCK, HOLDLOCK);", conn, tx);

        var v = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(v);
    }

    /// <summary>
    /// Modo SCAN:
    /// - Si existe (Numero + CodigoBarra + NumeroCaja) => Cantidad = Cantidad + addCantidad
    /// - Si no existe => inserta nueva línea con Item autogenerado y Costo desde INVENTARIO
    /// </summary>
    private static async Task<(int inserted, int updated)> AddOrIncrementAsync(
        SqlConnection conn,
        SqlTransaction tx,
        long numero,
        string codigoBarra,
        int numeroCaja,
        decimal addCantidad)
    {
        // Bloquea la fila si existe
        await using var cmdSel = new SqlCommand(@"
SELECT TOP 1 Item
FROM dbo.MOVDEVBORRADOR WITH (UPDLOCK, HOLDLOCK)
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@NumeroCaja;", conn, tx);

        cmdSel.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
        cmdSel.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra;
        cmdSel.Parameters.Add("@NumeroCaja", SqlDbType.Int).Value = numeroCaja;

        var existingItemObj = await cmdSel.ExecuteScalarAsync();

        if (existingItemObj != null)
        {
            await using var cmdUpd = new SqlCommand(@"
UPDATE dbo.MOVDEVBORRADOR
SET Cantidad = Cantidad + @AddCantidad
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@NumeroCaja;", conn, tx);

            cmdUpd.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
            cmdUpd.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra;
            cmdUpd.Parameters.Add("@NumeroCaja", SqlDbType.Int).Value = numeroCaja;

            var pAdd = cmdUpd.Parameters.Add("@AddCantidad", SqlDbType.Decimal);
            pAdd.Precision = 18; pAdd.Scale = 2;
            pAdd.Value = addCantidad;

            var rows = await cmdUpd.ExecuteNonQueryAsync();
            return (0, rows > 0 ? 1 : 0);
        }
        else
        {
            var nextItem = await GetNextItemAsync(conn, tx, numero);
            var costo = await GetCostoInventarioAsync(conn, tx, codigoBarra);

            await using var cmdIns = new SqlCommand(@"
INSERT INTO dbo.MOVDEVBORRADOR (Numero, CodigoBarra, Cantidad, NumeroCaja, Item, Costo)
VALUES (@Numero, @CodigoBarra, @Cantidad, @NumeroCaja, @Item, @Costo);", conn, tx);

            cmdIns.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
            cmdIns.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra;

            var pCant = cmdIns.Parameters.Add("@Cantidad", SqlDbType.Decimal);
            pCant.Precision = 18; pCant.Scale = 2;
            pCant.Value = addCantidad;

            cmdIns.Parameters.Add("@NumeroCaja", SqlDbType.Int).Value = numeroCaja;
            cmdIns.Parameters.Add("@Item", SqlDbType.Int).Value = nextItem;

            var pCosto = cmdIns.Parameters.Add("@Costo", SqlDbType.Decimal);
            pCosto.Precision = 18; pCosto.Scale = 8;
            pCosto.Value = costo;

            await cmdIns.ExecuteNonQueryAsync();
            return (1, 0);
        }
    }

    private static object ConflictLocked(long numero) =>
        new { ok = false, code = "APPROVED_LOCKED", error = $"El número {numero} está aprobado (Status=1) y no se puede editar." };

    // =============== DTOs ===============

    // NUEVO contrato: Item y Costo ya no se piden.
    // Cantidad y NumeroCaja son opcionales: default Cantidad=1, NumeroCaja=1
    public record ScanLineRequest(
        string CodigoBarra,
        decimal? Cantidad,
        int? NumeroCaja
    );

    public record CreateRequest(
        DateTime? Fecha,
        string? Observacion,
        string? Usuario,
        int? Status,
        List<ScanLineRequest> Detalles
    );

    public record ScanRequest(List<ScanLineRequest> Detalles);

    public record RunRequest(long? Numero, bool Aprobar);

    // Edición manual (SET, no suma):
    public record UpdateLineRequest(
        string CodigoBarra,
        decimal Cantidad,
        int NumeroCaja,
        int? NumeroCajaOriginal
    );

    public record UpdateDetallesRequest(List<UpdateLineRequest> Detalles);

    public record DevBorradorListItem(
        long Numero,
        DateTime? Fecha,
        string? Observacion,
        string? Usuario,
        int? Status
    );

    [HttpGet("ping")]
    public Task<IActionResult> Ping()
    {
        return Health();
    }

    [HttpGet("health")]
    public async Task<IActionResult> Health()
    {
        var cs = _config.GetConnectionString("SqlServer");
        var timeout = _config.GetValue<int?>("DatabaseDiagnostics:ConnectTimeoutSeconds") ?? 3;
        var diagnostic = await SqlConnectionDiagnostics.CheckAsync(cs, timeout, HttpContext.RequestAborted);

        return Ok(new
        {
            Ok = true,
            Service = "Borrador Dev",
            ListenerOk = true,
            diagnostic.DatabaseOk,
            DatabaseMessage = diagnostic.Message,
            DatabaseErrorType = diagnostic.ErrorType,
            DatabaseServer = diagnostic.Server,
            DatabaseName = diagnostic.Database,
            DatabaseAuthentication = diagnostic.Authentication,
            diagnostic.SqlErrorNumber,
            ServerTime = DateTime.Now
        });
    }
    [HttpGet("/api/Articulo/{codigoBarra}")]
    public async Task<IActionResult> GetArticulo(string codigoBarra)
    {
        if (string.IsNullOrWhiteSpace(codigoBarra))
        {
            return BadRequest(new { ok = false, error = "CodigoBarra es requerido." });
        }

        var cs = _config.GetConnectionString("SqlServer");
        await using var conn = new SqlConnection(cs);
        await conn.OpenAsync();

        const string sql = @"
SELECT
    I.Nombre,
    I.Existencia AS Existencia,
    I.CodigoBarra,
    M.Codigo AS CodigoMarca,
    M.Nombre AS NombreMarca,
    ROUND(I.PrecioDetal + (I.PrecioDetal * IMP.PorcentajeImpuesto / 100), 2)
        / (SELECT TOP 1 Valor FROM dbo.TASA_CAMBIO ORDER BY Fecha DESC) AS PrecioDolaresDetal,
    I.CostoDolar AS PrecioDolaresMayor,
    ROUND(I.PrecioDetal + (I.PrecioDetal * IMP.PorcentajeImpuesto / 100), 2) AS PrecioDetal,
    ROUND(I.PrecioMayor + (I.PrecioMayor * IMP.PorcentajeImpuesto / 100), 2) AS PrecioMayor,
    ROUND(I.PrecioAfiliado + (I.PrecioAfiliado * IMP.PorcentajeImpuesto / 100), 2) AS PrecioAfiliado,
    ROUND(I.PrecioPromocion + (I.PrecioPromocion * IMP.PorcentajeImpuesto / 100), 2) AS PrecioPromocion,
    CASE
        WHEN (I.Promocion = 1)
         AND (CONVERT(VARCHAR(10), GETDATE(), 103) BETWEEN I.FechaInicial AND I.FechaFinal)
        THEN 1 ELSE 0
    END AS PromocionActiva,
    CASE
        WHEN (I.Promocion = 1)
         AND (CONVERT(VARCHAR(10), GETDATE(), 103) BETWEEN I.FechaInicial AND I.FechaFinal)
        THEN (1 - ROUND((I.PrecioPromocion / I.PrecioDetal), 2)) * 100
        ELSE 0
    END AS PorcentajeDescuento,
    IMP.PorcentajeImpuesto AS TasaImpuesto
FROM dbo.INVENTARIO I
INNER JOIN dbo.IMPUESTOS IMP ON IMP.Codigo = I.TipoImpuesto
INNER JOIN dbo.MARCAS M ON M.Codigo = I.CodigoMarca
WHERE I.CodigoBarra = @CodigoBarra;";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigoBarra.Trim();

        var articulos = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            articulos.Add(new
            {
                nombre = reader.IsDBNull(0) ? null : reader.GetString(0),
                existencia = reader.IsDBNull(1) ? 0m : reader.GetDecimal(1),
                codigoBarra = reader.IsDBNull(2) ? null : reader.GetString(2),
                codigoMarca = reader.IsDBNull(3) ? null : reader.GetString(3),
                marca = reader.IsDBNull(4) ? null : reader.GetString(4),
                precioDolaresDetal = reader.IsDBNull(5) ? 0m : reader.GetDecimal(5),
                precioDolaresMayor = reader.IsDBNull(6) ? 0m : reader.GetDecimal(6),
                precioDetal = reader.IsDBNull(7) ? 0m : reader.GetDecimal(7),
                precioMayor = reader.IsDBNull(8) ? 0m : reader.GetDecimal(8),
                precioAfiliado = reader.IsDBNull(9) ? 0m : reader.GetDecimal(9),
                precioPromocion = reader.IsDBNull(10) ? 0m : reader.GetDecimal(10),
                promocionActiva = reader.IsDBNull(11) ? 0 : reader.GetInt32(11),
                porcentajeDescuento = reader.IsDBNull(12) ? 0m : reader.GetDecimal(12),
                tasaImpuesto = reader.IsDBNull(13) ? 0m : reader.GetDecimal(13),
                referencia = (string?)null,
                tasaDolares = 0m,
                impuestoMonto = 0m
            });
        }

        return Ok(articulos);
    }

    // ==============================
    // 0) LISTA / BUSCADOR
    // GET /borrador-dev/list?top=50&q=...&status=0
    // ==============================
    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] string? q = null, [FromQuery] int top = 50, [FromQuery] int? status = null)
    {
        try
        {
            if (top < 1) top = 1;
            if (top > 200) top = 200;

            var cs = _config.GetConnectionString("SqlServer");
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            var sql = @"
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
ORDER BY Numero DESC;";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.Add("@Top", SqlDbType.Int).Value = top;
            cmd.Parameters.Add("@Q", SqlDbType.NVarChar, 200).Value = (object?)q ?? DBNull.Value;
            cmd.Parameters.Add("@Status", SqlDbType.Int).Value = (object?)status ?? DBNull.Value;

            var items = new List<DevBorradorListItem>();
            await using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                var numero = Convert.ToInt64(r.GetValue(0));
                DateTime? fecha = r.IsDBNull(1) ? null : (DateTime?)r.GetValue(1);
                var observacion = r.IsDBNull(2) ? null : Convert.ToString(r.GetValue(2));
                var usuario = r.IsDBNull(3) ? null : Convert.ToString(r.GetValue(3));
                int? st = r.IsDBNull(4) ? null : Convert.ToInt32(r.GetValue(4));

                items.Add(new DevBorradorListItem(numero, fecha, observacion, usuario, st));
            }

            return Ok(new { ok = true, items });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    // ==============================
    // 1) CREATE (BOTON "NUEVO")
    // Crea cabecera + aplica lógica SCAN para la primera(s) línea(s)
    // ==============================
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateRequest req)
    {
        try
        {
            if (req.Detalles == null || req.Detalles.Count == 0)
                return BadRequest(new { ok = false, error = "Debe enviar al menos 1 detalle en Detalles." });

            var cs = _config.GetConnectionString("SqlServer");
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var txObj = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
            var tx = (SqlTransaction)txObj;

            // Validar códigos en INVENTARIO
            foreach (var d in req.Detalles)
            {
                var codigo = NormCode(d.CodigoBarra);
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    await txObj.RollbackAsync();
                    return BadRequest(new { ok = false, error = "CodigoBarra es requerido." });
                }

                if (!await CodigoExisteEnInventarioAsync(conn, tx, codigo))
                {
                    await txObj.RollbackAsync();
                    return BadRequest(new { ok = false, error = $"El CódigoBarra '{codigo}' no existe en INVENTARIO." });
                }
            }

            var numero = await GetNextNumeroAsync(conn, tx);

            var fecha = req.Fecha ?? DateTime.Now;
            var status = req.Status ?? 0;
            var obs = req.Observacion ?? "";
            var user = req.Usuario ?? "API";

            // Cabecera
            await using (var cmdIns = new SqlCommand(@"
INSERT INTO dbo.DEVBORRADOR (Numero, Fecha, Observacion, Usuario, Status)
VALUES (@Numero, @Fecha, @Observacion, @Usuario, @Status);", conn, tx))
            {
                cmdIns.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                cmdIns.Parameters.Add("@Fecha", SqlDbType.DateTime).Value = fecha;
                cmdIns.Parameters.Add("@Observacion", SqlDbType.NVarChar, -1).Value = obs;
                cmdIns.Parameters.Add("@Usuario", SqlDbType.NVarChar, -1).Value = user;
                cmdIns.Parameters.Add("@Status", SqlDbType.Int).Value = status;

                await cmdIns.ExecuteNonQueryAsync();
            }

            // Detalles con lógica SCAN (y costo desde inventario cuando se inserta)
            int inserted = 0, updated = 0;

            foreach (var d in req.Detalles)
            {
                var codigo = NormCode(d.CodigoBarra);
                var caja = NormCaja(d.NumeroCaja);
                var cant = NormCantidad(d.Cantidad);

                var (ins, upd) = await AddOrIncrementAsync(conn, tx, numero, codigo, caja, cant);
                inserted += ins;
                updated += upd;
            }

            await txObj.CommitAsync();
            return Ok(new { ok = true, numero, inserted, updated, detalles = req.Detalles.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    // ==============================
    // 1.1) SCAN (AGREGAR/INCREMENTAR) AL MISMO NÚMERO
    // POST /borrador-dev/{numero}/scan
    // ==============================
    [HttpPost("{numero:long}/scan")]
    public async Task<IActionResult> Scan(long numero, [FromBody] ScanRequest req)
    {
        try
        {
            if (req.Detalles == null || req.Detalles.Count == 0)
                return BadRequest(new { ok = false, error = "Debe enviar al menos 1 detalle." });

            var cs = _config.GetConnectionString("SqlServer");
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            // Status check antes
            var st = await GetStatusAsync(conn, tx: null, numero);
            if (st == null)
                return NotFound(new { ok = false, error = $"El número {numero} no existe." });

            if (st.Value == 1)
                return StatusCode(409, ConflictLocked(numero));

            await using var txObj = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
            var tx = (SqlTransaction)txObj;

            // Status check dentro (anti-carrera)
            var st2 = await GetStatusAsync(conn, tx, numero);
            if (st2 == null)
            {
                await txObj.RollbackAsync();
                return NotFound(new { ok = false, error = $"El número {numero} no existe." });
            }
            if (st2.Value == 1)
            {
                await txObj.RollbackAsync();
                return StatusCode(409, ConflictLocked(numero));
            }

            // Validar códigos
            foreach (var d in req.Detalles)
            {
                var codigo = NormCode(d.CodigoBarra);
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    await txObj.RollbackAsync();
                    return BadRequest(new { ok = false, error = "CodigoBarra es requerido." });
                }

                if (!await CodigoExisteEnInventarioAsync(conn, tx, codigo))
                {
                    await txObj.RollbackAsync();
                    return BadRequest(new { ok = false, error = $"El CódigoBarra '{codigo}' no existe en INVENTARIO." });
                }
            }

            int inserted = 0, updated = 0;

            foreach (var d in req.Detalles)
            {
                var codigo = NormCode(d.CodigoBarra);
                var caja = NormCaja(d.NumeroCaja);
                var cant = NormCantidad(d.Cantidad);

                var (ins, upd) = await AddOrIncrementAsync(conn, tx, numero, codigo, caja, cant);
                inserted += ins;
                updated += upd;
            }

            await txObj.CommitAsync();
            return Ok(new { ok = true, numero, inserted, updated, detalles = req.Detalles.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    // ==============================
    // 2) GET detalle por número (incluye Referencia y Nombre desde INVENTARIO)
    // ==============================
    [HttpGet("{numero:long}")]
    public async Task<IActionResult> Get(long numero)
    {
        try
        {
            var cs = _config.GetConnectionString("SqlServer");
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            object? header = null;

            await using (var cmdH = new SqlCommand(@"
SELECT TOP 1 Numero, Fecha, Observacion, Usuario, Status
FROM dbo.DEVBORRADOR
WHERE Numero=@Numero;", conn))
            {
                cmdH.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;

                await using var r = await cmdH.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    header = new
                    {
                        Numero = r.IsDBNull(0) ? null : r.GetValue(0),
                        Fecha = r.IsDBNull(1) ? null : r.GetValue(1),
                        Observacion = r.IsDBNull(2) ? null : r.GetValue(2),
                        Usuario = r.IsDBNull(3) ? null : r.GetValue(3),
                        Status = r.IsDBNull(4) ? null : r.GetValue(4)
                    };
                }
            }

            if (header == null)
                return NotFound(new { ok = false, error = $"El número {numero} no existe." });

            var detalles = new List<object>();

            await using (var cmdD = new SqlCommand(@"
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
ORDER BY m.Item;", conn))
            {
                cmdD.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;

                await using var r = await cmdD.ExecuteReaderAsync();
                while (await r.ReadAsync())
                {
                    detalles.Add(new
                    {
                        Numero = r.IsDBNull(0) ? null : r.GetValue(0),
                        CodigoBarra = r.IsDBNull(1) ? null : r.GetValue(1),
                        Referencia = r.IsDBNull(2) ? null : r.GetValue(2),
                        Nombre = r.IsDBNull(3) ? null : r.GetValue(3),
                        Cantidad = r.IsDBNull(4) ? null : r.GetValue(4),
                        NumeroCaja = r.IsDBNull(5) ? null : r.GetValue(5),
                        Item = r.IsDBNull(6) ? null : r.GetValue(6),
                        Costo = r.IsDBNull(7) ? null : r.GetValue(7)
                    });
                }
            }

            return Ok(new { ok = true, header, detalles });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    // ==============================
    // 3) Ejecuta SP (aprueba)
    // ==============================
    [HttpPost("run")]
    public async Task<IActionResult> Run([FromBody] RunRequest req)
    {
        try
        {
            var cs = _config.GetConnectionString("SqlServer");
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            // SP usa @Numero INT
            object numeroParam;
            if (req.Numero.HasValue)
            {
                if (req.Numero.Value > int.MaxValue || req.Numero.Value < int.MinValue)
                    return BadRequest(new { ok = false, error = $"El número {req.Numero.Value} excede el rango INT del procedimiento almacenado." });

                numeroParam = Convert.ToInt32(req.Numero.Value);
            }
            else
            {
                numeroParam = DBNull.Value;
            }

            await using var cmd = new SqlCommand("dbo.Borrador_Dev", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@Numero", SqlDbType.Int) { Value = numeroParam });
            cmd.Parameters.Add(new SqlParameter("@MarcarProcesado", SqlDbType.Bit) { Value = req.Aprobar ? 1 : 0 });

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!reader.HasRows) return Ok(new { ok = true, info = (object?)null });

            await reader.ReadAsync();
            var info = new Dictionary<string, object?>();
            for (int i = 0; i < reader.FieldCount; i++)
                info[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

            return Ok(new { ok = true, info });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }

    // ==============================
    // 4) UPDATE detalle editable (SET manual desde DetalleScreen)
    //    REQUERIMIENTO: si Status=1 -> NO SE PUEDE EDITAR
    //    + Si toca insertar nueva línea, Costo viene de INVENTARIO
    // ==============================
    [HttpPut("{numero:long}/detalle")]
    public async Task<IActionResult> UpdateDetalle(long numero, [FromBody] UpdateDetallesRequest req)
    {
        try
        {
            if (req.Detalles == null || req.Detalles.Count == 0)
                return BadRequest(new { ok = false, error = "Debe enviar al menos 1 detalle." });

            var cs = _config.GetConnectionString("SqlServer");
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            // Status check antes
            var st = await GetStatusAsync(conn, tx: null, numero);
            if (st == null)
                return NotFound(new { ok = false, error = $"El número {numero} no existe." });

            if (st.Value == 1)
                return StatusCode(409, ConflictLocked(numero));

            await using var txObj = await conn.BeginTransactionAsync(IsolationLevel.Serializable);
            var tx = (SqlTransaction)txObj;

            // Status check dentro
            var st2 = await GetStatusAsync(conn, tx, numero);
            if (st2 == null)
            {
                await txObj.RollbackAsync();
                return NotFound(new { ok = false, error = $"El número {numero} no existe." });
            }
            if (st2.Value == 1)
            {
                await txObj.RollbackAsync();
                return StatusCode(409, ConflictLocked(numero));
            }

            // Validar
            foreach (var d in req.Detalles)
            {
                var codigo = NormCode(d.CodigoBarra);
                if (string.IsNullOrWhiteSpace(codigo))
                {
                    await txObj.RollbackAsync();
                    return BadRequest(new { ok = false, error = "CodigoBarra es requerido." });
                }

                if (!await CodigoExisteEnInventarioAsync(conn, tx, codigo))
                {
                    await txObj.RollbackAsync();
                    return BadRequest(new { ok = false, error = $"El CódigoBarra '{codigo}' no existe en INVENTARIO." });
                }

                if (d.Cantidad <= 0)
                {
                    await txObj.RollbackAsync();
                    return BadRequest(new { ok = false, error = "Cantidad debe ser > 0." });
                }

                if (d.NumeroCaja <= 0)
                {
                    await txObj.RollbackAsync();
                    return BadRequest(new { ok = false, error = "NumeroCaja debe ser >= 1." });
                }
            }

            int updated = 0, inserted = 0, deleted = 0;

            foreach (var d in req.Detalles)
            {
                var codigo = NormCode(d.CodigoBarra);
                var caja = d.NumeroCaja;
                var cajaOrig = d.NumeroCajaOriginal ?? caja;
                var cant = d.Cantidad;

                // Bloquea fila origen si existe
                await using var cmdSel = new SqlCommand(@"
SELECT TOP 1 Item
FROM dbo.MOVDEVBORRADOR WITH (UPDLOCK, HOLDLOCK)
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@CajaOrig;", conn, tx);

                cmdSel.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                cmdSel.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;
                cmdSel.Parameters.Add("@CajaOrig", SqlDbType.Int).Value = cajaOrig;

                var existsOrigin = await cmdSel.ExecuteScalarAsync() != null;

                if (!existsOrigin)
                {
                    // No existe -> insertar como nueva línea (Costo desde INVENTARIO)
                    var nextItem = await GetNextItemAsync(conn, tx, numero);
                    var costo = await GetCostoInventarioAsync(conn, tx, codigo);

                    await using var cmdIns = new SqlCommand(@"
INSERT INTO dbo.MOVDEVBORRADOR (Numero, CodigoBarra, Cantidad, NumeroCaja, Item, Costo)
VALUES (@Numero, @CodigoBarra, @Cantidad, @NumeroCaja, @Item, @Costo);", conn, tx);

                    cmdIns.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                    cmdIns.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;

                    var pCant = cmdIns.Parameters.Add("@Cantidad", SqlDbType.Decimal);
                    pCant.Precision = 18; pCant.Scale = 2;
                    pCant.Value = cant;

                    cmdIns.Parameters.Add("@NumeroCaja", SqlDbType.Int).Value = caja;
                    cmdIns.Parameters.Add("@Item", SqlDbType.Int).Value = nextItem;

                    var pCosto = cmdIns.Parameters.Add("@Costo", SqlDbType.Decimal);
                    pCosto.Precision = 18; pCosto.Scale = 8;
                    pCosto.Value = costo;

                    await cmdIns.ExecuteNonQueryAsync();
                    inserted++;
                    continue;
                }

                if (cajaOrig == caja)
                {
                    // Solo set de cantidad en la misma caja
                    await using var cmdUpd = new SqlCommand(@"
UPDATE dbo.MOVDEVBORRADOR
SET Cantidad=@Cantidad
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@NumeroCaja;", conn, tx);

                    cmdUpd.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                    cmdUpd.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;
                    cmdUpd.Parameters.Add("@NumeroCaja", SqlDbType.Int).Value = caja;

                    var pCant = cmdUpd.Parameters.Add("@Cantidad", SqlDbType.Decimal);
                    pCant.Precision = 18; pCant.Scale = 2;
                    pCant.Value = cant;

                    var rows = await cmdUpd.ExecuteNonQueryAsync();
                    if (rows > 0) updated++;
                }
                else
                {
                    // Mover de caja: si ya existe destino, merge (suma) y borra origen.
                    await using var cmdSelDest = new SqlCommand(@"
SELECT 1
FROM dbo.MOVDEVBORRADOR WITH (UPDLOCK, HOLDLOCK)
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@CajaDest;", conn, tx);

                    cmdSelDest.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                    cmdSelDest.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;
                    cmdSelDest.Parameters.Add("@CajaDest", SqlDbType.Int).Value = caja;

                    var existsDest = await cmdSelDest.ExecuteScalarAsync() != null;

                    if (existsDest)
                    {
                        // Suma al destino
                        await using var cmdUpdDest = new SqlCommand(@"
UPDATE dbo.MOVDEVBORRADOR
SET Cantidad = Cantidad + @AddCantidad
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@CajaDest;", conn, tx);

                        cmdUpdDest.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                        cmdUpdDest.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;
                        cmdUpdDest.Parameters.Add("@CajaDest", SqlDbType.Int).Value = caja;

                        var pAdd = cmdUpdDest.Parameters.Add("@AddCantidad", SqlDbType.Decimal);
                        pAdd.Precision = 18; pAdd.Scale = 2;
                        pAdd.Value = cant;

                        var r1 = await cmdUpdDest.ExecuteNonQueryAsync();
                        if (r1 > 0) updated++;

                        // Borra origen
                        await using var cmdDel = new SqlCommand(@"
DELETE FROM dbo.MOVDEVBORRADOR
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@CajaOrig;", conn, tx);

                        cmdDel.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                        cmdDel.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;
                        cmdDel.Parameters.Add("@CajaOrig", SqlDbType.Int).Value = cajaOrig;

                        var r2 = await cmdDel.ExecuteNonQueryAsync();
                        if (r2 > 0) deleted++;
                    }
                    else
                    {
                        // Cambia caja + set cantidad (Costo no se toca)
                        await using var cmdUpdMove = new SqlCommand(@"
UPDATE dbo.MOVDEVBORRADOR
SET NumeroCaja=@CajaDest, Cantidad=@Cantidad
WHERE Numero=@Numero AND CodigoBarra=@CodigoBarra AND NumeroCaja=@CajaOrig;", conn, tx);

                        cmdUpdMove.Parameters.Add("@Numero", SqlDbType.BigInt).Value = numero;
                        cmdUpdMove.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = codigo;
                        cmdUpdMove.Parameters.Add("@CajaOrig", SqlDbType.Int).Value = cajaOrig;
                        cmdUpdMove.Parameters.Add("@CajaDest", SqlDbType.Int).Value = caja;

                        var pCant = cmdUpdMove.Parameters.Add("@Cantidad", SqlDbType.Decimal);
                        pCant.Precision = 18; pCant.Scale = 2;
                        pCant.Value = cant;

                        var rows = await cmdUpdMove.ExecuteNonQueryAsync();
                        if (rows > 0) updated++;
                    }
                }
            }

            await txObj.CommitAsync();
            return Ok(new { ok = true, numero, updated, inserted, deleted });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }
}
