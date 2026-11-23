using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BackBorradorDev.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LoginController : ControllerBase
{
    private readonly IConfiguration config;
    private readonly TomaFisicaSessionStore sessions;

    public LoginController(IConfiguration config, TomaFisicaSessionStore sessions)
    {
        this.config = config;
        this.sessions = sessions;
    }

    public sealed record LoginRequest(string? Usuario, string? Password);

    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest? request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Usuario) || request.Password is null)
            return Unauthorized();

        const string sql = @"
SELECT TOP (1)
    U.CodUsuario,
    U.Pasword,
    CAST(U.Pasword AS VARBINARY(200)) AS PaswordBytes
FROM dbo.USUARIOS AS U
INNER JOIN dbo.USUARIOGRUPO AS UG ON UG.CodUsuario = U.CodUsuario
WHERE (U.CodUsuario = @Usuario OR U.NombreUsuario = @Usuario)
  AND U.Status = 1
  AND UG.CodGrupo = 'INVE';";

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Usuario", SqlDbType.NVarChar, 100).Value = request.Usuario.Trim();

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return Unauthorized();

        var userCode = reader.IsDBNull(0) ? null : reader.GetString(0);
        var storedPassword = reader.IsDBNull(1) ? null : reader.GetString(1);
        var storedPasswordBytes = reader.IsDBNull(2) ? null : (byte[])reader.GetValue(2);

        // Mantiene usuarios con contraseña en texto y admite el formato legacy.
        var passwordIsValid = string.Equals(storedPassword, request.Password, StringComparison.OrdinalIgnoreCase)
            || PasswordLegacyCodec.Matches(request.Password, storedPasswordBytes);

        if (string.IsNullOrWhiteSpace(userCode) || !passwordIsValid)
            return Unauthorized();

        return Ok(new
        {
            CodUsuario = userCode,
            Token = sessions.Create(userCode),
            ExpiraEnHoras = TomaFisicaSessionStore.DurationHours,
            Mensaje = "Inicio de sesión correcto."
        });
    }

    [HttpPost("cerrar")]
    public IActionResult Close()
    {
        sessions.Close(GetBearerToken());
        return Ok(new { Mensaje = "Sesión cerrada." });
    }

    private string GetConnectionString() =>
        config.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("No está configurado ConnectionStrings:SqlServer.");

    private string? GetBearerToken()
    {
        var authorization = Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        return authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? authorization[prefix.Length..].Trim()
            : null;
    }
}