using Microsoft.AspNetCore.Mvc;

namespace BackBorradorDev.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ValoresController : ControllerBase
{
    private readonly IConfiguration config;

    public ValoresController(IConfiguration config)
    {
        this.config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var rows = await SqlStoredProcedureReader.ExecuteAsync(
            GetConnectionString(),
            "dbo.ApiBuscarValores");
        return Ok(rows);
    }

    private string GetConnectionString() =>
        config.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("No está configurado ConnectionStrings:SqlServer.");
}
