using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Onboarding;
using ThreadFlow.Domain.Modules.PlatformAdministration;
using ThreadFlow.Infrastructure.data.Master;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Route("api/onboarding/applications")]
public sealed class OnboardingController(
    MasterDbContext masterDb,
    ILogger<OnboardingController> logger) : ControllerBase
{
    private static readonly string[] CompanySizes = ["Micro", "Small", "Medium"];

    [HttpPost]
    [ProducesResponseType<CompanyApplicationSubmittedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CompanyApplicationSubmittedResponse>> Submit(
        [FromBody] SubmitCompanyApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var size = CompanySizes.FirstOrDefault(value =>
            string.Equals(value, request.RequestedCompanySize.Trim(), StringComparison.OrdinalIgnoreCase));
        if (size is null)
        {
            return BadRequest(new
            {
                error = "Choose one of the available company plans.",
                availablePlans = CompanySizes
            });
        }

        var email = request.ContactEmail.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        if (await masterDb.Users.AnyAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken) ||
            await masterDb.CompanyApplications.AnyAsync(
                application => application.Status == "Pending" &&
                               application.ContactEmail.ToUpper() == normalizedEmail,
                cancellationToken))
        {
            return Conflict(new { error = "That email already belongs to a ThreadFlow account." });
        }

        var application = new CompanyApplication
        {
            CompanyName = request.CompanyName.Trim(),
            ContactName = request.ContactName.Trim(),
            ContactEmail = email,
            ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone)
                ? null
                : request.ContactPhone.Trim(),
            RequestedCompanySize = size,
            Status = "Pending",
            SubmittedAtUtc = DateTime.UtcNow
        };
        masterDb.CompanyApplications.Add(application);
        masterDb.PlatformAuditEntries.Add(new PlatformAuditEntry
        {
            ActorUserId = null,
            Action = "CompanyApplicationSubmitted",
            EntityType = "CompanyApplication",
            EntityId = application.Id.ToString(),
            Summary = $"{application.CompanyName} applied for the {size} plan."
        });
        await masterDb.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Company application {ApplicationId} submitted for plan {PlanCode}.",
            application.Id,
            size);

        var response = new CompanyApplicationSubmittedResponse(
            application.Id,
            application.Status,
            application.SubmittedAtUtc);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
