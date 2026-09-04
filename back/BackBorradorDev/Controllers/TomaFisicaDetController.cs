using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace BackBorradorDev.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TomaFisicaDetController : ControllerBase
{
    private readonly IConfiguration config;

    public TomaFisicaDetController(IConfiguration config)
    {
        this.config = config;
    }

    [HttpGet("byParameters")]
    public async Task<IActionResult> Get(
        [FromQuery] int numeroPagina,
        [FromQuery] string tipo,
        [FromQuery] string imei)
    {
        var procedure = string.Equals(tipo, "DETALLE", StringComparison.OrdinalIgnoreCase)
            ? "dbo.ApiBuscarTomaFisicaDetalle"
            : "dbo.ApiBuscarTomaFisicaTotal";

        var rows = await SqlStoredProcedureReader.ExecuteAsync(
            GetConnectionString(),
            procedure,
            ("@NumeroPagina", SqlDbType.Int, numeroPagina),
            ("@Imei", SqlDbType.NVarChar, imei));
        return Ok(rows);
    }

    private string GetConnectionString() =>
        config.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("No está configurado ConnectionStrings:SqlServer.");
}
