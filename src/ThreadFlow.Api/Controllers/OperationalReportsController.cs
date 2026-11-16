using Microsoft.AspNetCore.Mvc;
using ThreadFlow.Api.Modules.ManagementApprovalsReporting;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class OperationalReportsController(
    GetSalesOperationalReport getSalesOperationalReport) : ControllerBase
{
    [HttpGet("sales")]
    public async Task<ActionResult<SalesOperationalReport>> GetSales(
        [FromQuery] DateTimeOffset fromUtc,
        [FromQuery] DateTimeOffset toUtcExclusive,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await getSalesOperationalReport.ExecuteAsync(
                new GetSalesOperationalReportQuery(fromUtc, toUtcExclusive),
                cancellationToken);

            return Ok(report);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}