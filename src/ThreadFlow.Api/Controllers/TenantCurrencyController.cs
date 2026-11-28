using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Infrastructure.Tenancy;
using ThreadFlow.Infrastructure.data.Master;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ThreadFlowRoles.TenantWorkspaceUsers)]
[Route("api/tenant/currency")]
public sealed class TenantCurrencyController(
    MasterDbContext masterDb,
    ICurrentTenantContext currentTenant) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TenantCurrencyResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantCurrencyResponse>> Get(CancellationToken cancellationToken)
    {
        var currencyCode = await masterDb.TenantCompanySettings
            .AsNoTracking()
            .Where(settings => settings.TenantId == currentTenant.TenantId)
            .Select(settings => settings.CurrencyCode)
            .FirstOrDefaultAsync(cancellationToken);

        if (currencyCode is null)
            return NotFound(new { error = "Company currency has not been configured for this tenant." });

        return Ok(new TenantCurrencyResponse(currencyCode));
    }
}

public sealed record TenantCurrencyResponse(string CurrencyCode);
