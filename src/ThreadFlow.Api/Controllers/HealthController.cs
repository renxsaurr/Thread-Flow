using Microsoft.AspNetCore.Mvc;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "ready", service = "ThreadFlow.Api" });
}
