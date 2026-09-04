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
SELECT TOP (1) U.CodUsuario
FROM dbo.USUARIOS AS U
INNER JOIN dbo.USUARIOGRUPO AS UG ON UG.CodUsuario = U.CodUsuario
WHERE (U.CodUsuario = @Usuario OR U.NombreUsuario = @Usuario)
  AND U.Pasword = @Password
  AND U.Status = 1
  AND UG.CodGrupo = 'INVE';";

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Usuario", SqlDbType.NVarChar, 100).Value = request.Usuario.Trim();
        command.Parameters.Add("@Password", SqlDbType.NVarChar, 200).Value = request.Password;

        var userCode = Convert.ToString(await command.ExecuteScalarAsync());
        if (string.IsNullOrWhiteSpace(userCode))
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
