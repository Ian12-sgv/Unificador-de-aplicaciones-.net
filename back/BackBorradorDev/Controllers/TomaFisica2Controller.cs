using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BackBorradorDev.Controllers;

[ApiController]
[Route("api/TomaFisica2")]
public sealed class TomaFisica2Controller : ControllerBase
{
    private readonly IConfiguration config;
    private readonly TomaFisicaSessionStore sessions;

    public TomaFisica2Controller(IConfiguration config, TomaFisicaSessionStore sessions)
    {
        this.config = config;
        this.sessions = sessions;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] TomaFisicaController.TomaFisicaRequest? request)
    {
        var userCode = GetAuthenticatedUser();
        if (userCode is null)
            return Unauthorized();

        if (request is null || string.IsNullOrWhiteSpace(request.CodigoBarra))
            return BadRequest();

        try
        {
            await using var connection = new SqlConnection(GetConnectionString());
            await connection.OpenAsync();
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();

            // El primer usuario que usa un numero de pagina es su propietario.
            // UPDLOCK/HOLDLOCK evita que dos PDA reserven el mismo numero al mismo tiempo.
            await using (var ownerCommand = new SqlCommand(@"
SELECT TOP (1) LTRIM(RTRIM(autor))
FROM dbo.TOMAFISICA2 WITH (UPDLOCK, HOLDLOCK)
WHERE NumeroPagina = @NumeroPagina
  AND NULLIF(LTRIM(RTRIM(autor)), '') IS NOT NULL;", connection, transaction))
            {
                ownerCommand.Parameters.Add("@NumeroPagina", SqlDbType.Int).Value = request.NumeroPagina;
                var existingOwner = Convert.ToString(await ownerCommand.ExecuteScalarAsync())?.Trim();
                if (!string.IsNullOrWhiteSpace(existingOwner) &&
                    !string.Equals(existingOwner, userCode, StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync();
                    return Conflict(new
                    {
                        code = "NUMERO_ASIGNADO_A_OTRO_USUARIO",
                        message = "Este numero de toma fisica pertenece a otro usuario."
                    });
                }
            }
            await using (var createCommand = new SqlCommand(@"
INSERT INTO dbo.TOMAFISICA2 (NumeroPagina, Fecha, Observacion, Usuario, Status, autor)
SELECT @NumeroPagina, @Fecha, @Observacion, @Usuario, @Status, @Autor
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.TOMAFISICA2 WITH (UPDLOCK, HOLDLOCK)
    WHERE NumeroPagina = @NumeroPagina);", connection, transaction))
            {
                createCommand.Parameters.Add("@NumeroPagina", SqlDbType.Int).Value = request.NumeroPagina;
                createCommand.Parameters.Add("@Fecha", SqlDbType.SmallDateTime).Value = request.Fecha;
                createCommand.Parameters.Add("@Observacion", SqlDbType.NVarChar, -1).Value = request.Observacion ?? string.Empty;
                createCommand.Parameters.Add("@Usuario", SqlDbType.NVarChar, 100).Value = userCode;
                createCommand.Parameters.Add("@Status", SqlDbType.TinyInt).Value = (byte)(request.Status is > 0 and <= byte.MaxValue ? request.Status : 1);
                createCommand.Parameters.Add("@Autor", SqlDbType.NVarChar, 100).Value = userCode;
                await createCommand.ExecuteNonQueryAsync();
            }
            await using (var command = new SqlCommand("dbo.ApiProcesarToma_DOS", connection, transaction)
            {
                CommandType = CommandType.StoredProcedure
            })
            {
                command.Parameters.Add("@CodigoBarra", SqlDbType.NVarChar, 100).Value = request.CodigoBarra.Trim();
                command.Parameters.Add("@NumeroPagina", SqlDbType.Int).Value = request.NumeroPagina;
                command.Parameters.Add("@Observacion", SqlDbType.NVarChar, -1).Value = request.Observacion ?? string.Empty;
                command.Parameters.Add("@Cantidad", SqlDbType.Int).Value = request.Cantidad;
                command.Parameters.Add("@Valor", SqlDbType.Bit).Value = request.Valor;
                await command.ExecuteNonQueryAsync();
            }

            await using (var command = new SqlCommand(@"
UPDATE dbo.TOMAFISICA2
SET autor = @Autor
WHERE NumeroPagina = @NumeroPagina
  AND (autor IS NULL OR LTRIM(RTRIM(autor)) = '' OR LTRIM(RTRIM(autor)) = @Autor);", connection, transaction))
            {
                command.Parameters.Add("@Autor", SqlDbType.NVarChar, 100).Value = userCode;
                command.Parameters.Add("@NumeroPagina", SqlDbType.Int).Value = request.NumeroPagina;
                if (await command.ExecuteNonQueryAsync() == 0)
                {
                    await transaction.RollbackAsync();
                    WriteProcessingError($"No se encontro una fila de TOMAFISICA2 para asignar el autor. Pagina: {request.NumeroPagina}. Codigo: {request.CodigoBarra}");
                    return Ok(-1);
                }
            }

            await transaction.CommitAsync();
            return Ok(1);
        }
        catch (Exception ex)
        {
            WriteProcessingError(ex.ToString());
            return Ok(-1);
        }
    }

    [HttpGet("byParameters")]
    public async Task<IActionResult> GetByParameters(
        [FromQuery] int numeroPagina,
        [FromQuery] string codigoBarra,
        [FromQuery] string imei)
    {
        if (GetAuthenticatedUser() is null)
            return Unauthorized();

        var rows = await SqlStoredProcedureReader.ExecuteAsync(
            GetConnectionString(),
            "dbo.ApiBuscarTomaFisicaPost_DOS",
            ("@NumeroPagina", SqlDbType.Int, numeroPagina),
            ("@CodigoBarra", SqlDbType.NVarChar, codigoBarra ?? string.Empty),
            ("@Imei", SqlDbType.NVarChar, imei ?? string.Empty));
        return Ok(rows);
    }

    [HttpGet("{imei}")]
    public async Task<IActionResult> Get(string imei)
    {
        try
        {
            var rows = await SqlStoredProcedureReader.ExecuteAsync(
                GetConnectionString(),
                "dbo.ApiBuscarTomaFisicaMae_DOS",
                ("@Imei", SqlDbType.NVarChar, imei ?? string.Empty));
            return Ok(rows);
        }
        catch (Exception ex)
        {
            WriteProcessingError("Error al listar Toma Fisica 2: " + ex);
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }    private string GetConnectionString() =>
        config.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("No está configurado ConnectionStrings:SqlServer.");

    private static void WriteProcessingError(string error)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "errores-toma-fisica2.log");
            System.IO.File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {error}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // El registro no debe impedir que se responda al PDA.
        }
    }
    private string? GetAuthenticatedUser()
    {
        var authorization = Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        return sessions.GetUser(authorization[prefix.Length..].Trim());
    }
}
