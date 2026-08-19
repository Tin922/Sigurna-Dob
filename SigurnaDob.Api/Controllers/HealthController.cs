using Microsoft.AspNetCore.Mvc;

namespace SigurnaDob.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() =>
        Ok(new { status = "ok", application = "SigurnaDob.Api" });
}
