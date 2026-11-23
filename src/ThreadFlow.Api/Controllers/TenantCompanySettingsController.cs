using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Infrastructure.Tenancy;
using ThreadFlow.Infrastructure.data.Master;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ThreadFlowRoles.TenantAdmin)]
[Route("api/tenant/company-settings")]
public sealed class TenantCompanySettingsController(
    MasterDbContext masterDb,
    ICurrentTenantContext currentTenant) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<TenantCompanySetupRequest>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TenantCompanySetupRequest>> Get(CancellationToken cancellationToken)
    {
        var settings = await masterDb.TenantCompanySettings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.TenantId == currentTenant.TenantId, cancellationToken);
        if (settings is null)
            return NotFound(new { error = "Company settings have not been created for this tenant." });

        return Ok(new TenantCompanySetupRequest
        {
            LegalName = settings.LegalName,
            BusinessAddress = settings.BusinessAddress,
            TaxIdentificationNumber = settings.TaxIdentificationNumber,
            ContactEmail = settings.ContactEmail,
            ContactPhone = settings.ContactPhone,
            TaxRatePercent = settings.TaxRatePercent,
            ReceiptHeader = settings.ReceiptHeader,
            ReceiptFooter = settings.ReceiptFooter,
            CurrencyCode = settings.CurrencyCode
        });
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromBody] TenantCompanySetupRequest request,
        CancellationToken cancellationToken)
    {
        var settings = await masterDb.TenantCompanySettings
            .FirstOrDefaultAsync(item => item.TenantId == currentTenant.TenantId, cancellationToken);
        if (settings is null)
            return NotFound(new { error = "Company settings have not been created for this tenant." });

        settings.LegalName = request.LegalName.Trim();
        settings.BusinessAddress = request.BusinessAddress.Trim();
        settings.TaxIdentificationNumber = request.TaxIdentificationNumber?.Trim();
        settings.ContactEmail = request.ContactEmail.Trim();
        settings.ContactPhone = request.ContactPhone?.Trim();
        settings.TaxRatePercent = request.TaxRatePercent;
        settings.ReceiptHeader = request.ReceiptHeader?.Trim();
        settings.ReceiptFooter = request.ReceiptFooter?.Trim();
        settings.CurrencyCode = request.CurrencyCode.Trim().ToUpperInvariant();
        settings.UpdatedAtUtc = DateTime.UtcNow;

        var actorId = Guid.TryParse(User.FindFirst("sub")?.Value, out var userId) ? userId : (Guid?)null;
        masterDb.PlatformAuditEntries.Add(new ThreadFlow.Domain.Modules.PlatformAdministration.PlatformAuditEntry
        {
            ActorUserId = actorId,
            TenantId = currentTenant.TenantId,
            Action = "TenantCompanySettingsUpdated",
            EntityType = "TenantCompanySettings",
            EntityId = currentTenant.TenantId.ToString(),
            Summary = $"Company profile, tax, or receipt settings were updated for {currentTenant.Code}."
        });

        await masterDb.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
