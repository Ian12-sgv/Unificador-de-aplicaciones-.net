using Microsoft.AspNetCore.Mvc;

namespace BackBorradorDev.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class Valores2Controller : ControllerBase
{
    private readonly IConfiguration config;

    public Valores2Controller(IConfiguration config)
    {
        this.config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var rows = await SqlStoredProcedureReader.ExecuteAsync(
            GetConnectionString(),
            "dbo.ApiBuscarValores_DOS");
        return Ok(rows);
    }

    private string GetConnectionString() =>
        config.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("No estÃ¡ configurado ConnectionStrings:SqlServer.");
}