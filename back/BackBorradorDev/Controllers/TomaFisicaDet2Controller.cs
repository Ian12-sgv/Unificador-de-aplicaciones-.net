using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace BackBorradorDev.Controllers;

[ApiController]
[Route("api/TomaFisicaDet2")]
public sealed class TomaFisicaDet2Controller : ControllerBase
{
    private readonly IConfiguration config;

    public TomaFisicaDet2Controller(IConfiguration config)
    {
        this.config = config;
    }

    [HttpGet("byParameters")]
    public async Task<IActionResult> Get([FromQuery] int numeroPagina, [FromQuery] string tipo, [FromQuery] string imei)
    {
        var procedure = string.Equals(tipo, "DETALLE", StringComparison.OrdinalIgnoreCase)
            ? "dbo.ApiBuscarTomaFisicaDetalle_DOS"
            : "dbo.ApiBuscarTomaFisicaTotal_DOS";
        var rows = await SqlStoredProcedureReader.ExecuteAsync(
            GetConnectionString(),
            procedure,
            ("@NumeroPagina", SqlDbType.Int, numeroPagina),
            ("@Imei", SqlDbType.NVarChar, imei ?? string.Empty));
        return Ok(rows);
    }

    private string GetConnectionString() =>
        config.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("No estÃ¡ configurado ConnectionStrings:SqlServer.");
}