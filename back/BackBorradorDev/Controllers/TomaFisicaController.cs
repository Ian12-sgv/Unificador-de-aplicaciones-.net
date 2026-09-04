using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BackBorradorDev.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TomaFisicaController : ControllerBase
{
    private readonly IConfiguration config;
    private readonly TomaFisicaSessionStore sessions;

    public TomaFisicaController(IConfiguration config, TomaFisicaSessionStore sessions)
    {
        this.config = config;
        this.sessions = sessions;
    }

    public sealed record TomaFisicaRequest(
        int NumeroPagina,
        DateTime Fecha,
        string? Observacion,
        string? Referencia,
        int Cantidad,
        string? Usuario,
        int Status,
        string? CodigoBarra,
        string? Prenda,
        string? Id,
        string? Text,
        string? Nombre,
        bool Valor,
        string? Imei,
        string? Autor);

    [HttpGet("{imei}")]
    public async Task<IActionResult> Get(string imei)
    {
        var rows = await SqlStoredProcedureReader.ExecuteAsync(
            GetConnectionString(),
            "dbo.ApiBuscarTomaFisicaMae",
            ("@Imei", SqlDbType.NVarChar, imei));
        return Ok(rows);
    }

    [HttpGet("byParameters")]
    public async Task<IActionResult> GetByParameters(
        [FromQuery] int numeroPagina,
        [FromQuery] string codigoBarra,
        [FromQuery] string imei)
    {
        var rows = await SqlStoredProcedureReader.ExecuteAsync(
            GetConnectionString(),
            "dbo.ApiBuscarTomaFisicaPost",
            ("@NumeroPagina", SqlDbType.Int, numeroPagina),
            ("@CodigoBarra", SqlDbType.NVarChar, codigoBarra),
            ("@Imei", SqlDbType.NVarChar, imei));
        return Ok(rows);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] TomaFisicaRequest? request)
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
FROM dbo.TOMAFISICA1 WITH (UPDLOCK, HOLDLOCK)
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
            await using (var command = new SqlCommand("dbo.ApiProcesarToma", connection, transaction)
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
UPDATE dbo.TOMAFISICA1
SET autor = @Autor
WHERE NumeroPagina = @NumeroPagina
  AND (autor IS NULL OR LTRIM(RTRIM(autor)) = '' OR LTRIM(RTRIM(autor)) = @Autor);", connection, transaction))
            {
                command.Parameters.Add("@Autor", SqlDbType.NVarChar, 100).Value = userCode;
                command.Parameters.Add("@NumeroPagina", SqlDbType.Int).Value = request.NumeroPagina;
                if (await command.ExecuteNonQueryAsync() == 0)
                {
                    await transaction.RollbackAsync();
                    return Ok(-1);
                }
            }

            await transaction.CommitAsync();
            return Ok(1);
        }
        catch
        {
            return Ok(-1);
        }
    }

    private string GetConnectionString() =>
        config.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("No está configurado ConnectionStrings:SqlServer.");

    private string? GetAuthenticatedUser()
    {
        var authorization = Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return null;

        return sessions.GetUser(authorization[prefix.Length..].Trim());
    }
}
