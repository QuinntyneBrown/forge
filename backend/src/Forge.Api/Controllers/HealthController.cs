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
    public IActionResult Live()
        => Ok(new { status = "Healthy" });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return new JsonResult(new { status = "Unhealthy" });
            }
            return Ok(new { status = "Healthy" });
        }
        catch
        {
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return new JsonResult(new { status = "Unhealthy" });
        }
    }
}
