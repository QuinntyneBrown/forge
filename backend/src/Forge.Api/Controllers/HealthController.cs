using Forge.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Forge.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            await _db.Database.CanConnectAsync(cancellationToken);
            return Ok(new { status = "Healthy" });
        }
        catch
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return new JsonResult(new { status = "Unhealthy" });
        }
    }
}
