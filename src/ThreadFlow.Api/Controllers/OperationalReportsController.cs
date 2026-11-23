using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Api.Modules.ManagementApprovalsReporting;
using ThreadFlow.Api.Tenancy;
using ThreadFlow.Infrastructure.Tenancy;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ThreadFlowRoles.TenantOwnerManager + "," + ThreadFlowRoles.BranchManager)]
[Route("api/reports")]
public sealed class OperationalReportsController(
    GetSalesOperationalReport getSalesOperationalReport,
    TenantBranchAssignmentService branchAssignments,
    ICurrentTenantContext currentTenant) : ControllerBase
{
    [HttpGet("sales")]
    public async Task<ActionResult<SalesOperationalReport>> GetSales(
        [FromQuery] DateTimeOffset fromUtc,
        [FromQuery] DateTimeOffset toUtcExclusive,
        [FromQuery] Guid? branchId,
        CancellationToken cancellationToken)
    {
        if (User.IsInRole(ThreadFlowRoles.BranchManager) &&
            string.Equals(currentTenant.CompanySize, "Medium", StringComparison.OrdinalIgnoreCase))
        {
            var assignedBranchId = await branchAssignments.GetAssignedBranchIdAsync(User, cancellationToken);
            if (assignedBranchId is null)
                return Conflict(new { error = "Your account needs an active branch assignment. Contact your Tenant Admin." });
            if (branchId is Guid requestedBranchId && requestedBranchId != assignedBranchId.Value)
                return Forbid();
            branchId = assignedBranchId;
        }

        try
        {
            var report = await getSalesOperationalReport.ExecuteAsync(
                new GetSalesOperationalReportQuery(fromUtc, toUtcExclusive, branchId),
                cancellationToken);

            return Ok(report);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
