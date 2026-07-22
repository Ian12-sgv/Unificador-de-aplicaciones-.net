using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BackBorradorDev.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SucursalesController : ControllerBase
{
    private readonly IConfiguration _config;

    public SucursalesController(IConfiguration config)
    {
        _config = config;
    }

    public record SucursalItem(string Codigo, string Nombre, string? Direccion, string? Telefono);

    [HttpGet]
    public async Task<ActionResult<List<SucursalItem>>> Get()
    {
        try
        {
            var cs = _config.GetConnectionString("SqlServer");
            if (string.IsNullOrWhiteSpace(cs))
                return StatusCode(500, new { ok = false, error = "No esta configurado ConnectionStrings:SqlServer." });

            var items = new List<SucursalItem>();
            await using var conn = new SqlConnection(cs);
            await conn.OpenAsync();

            await using var cmd = new SqlCommand(@"
SELECT Codigo, Nombre, Direccion, Telefono
FROM dbo.sucursales
WHERE ISNULL(status, 1) = 1
ORDER BY Nombre;", conn);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                items.Add(new SucursalItem(
                    Convert.ToString(reader["Codigo"]) ?? "",
                    Convert.ToString(reader["Nombre"]) ?? "",
                    reader["Direccion"] as string,
                    reader["Telefono"] as string
                ));
            }

            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { ok = false, error = ex.Message, inner = ex.InnerException?.Message });
        }
    }
}
