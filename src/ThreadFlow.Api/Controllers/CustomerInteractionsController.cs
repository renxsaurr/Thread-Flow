using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Api.Modules.CustomerServiceAndLoyalty;
using ThreadFlow.Api.Tenancy;
using ThreadFlow.Domain.Modules.Crm;
using ThreadFlow.Infrastructure.Tenancy;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ThreadFlowRoles.CustomerServiceAgent)]
[Route("api/customer-interactions")]
public sealed class CustomerInteractionsController(
    CreateCustomerInteraction createCustomerInteraction,
    GetCustomerInteractions getCustomerInteractions,
    TenantBranchAssignmentService branchAssignments,
    ICurrentTenantContext currentTenant) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CustomerInteractionResponse>> Create(
        [FromBody] CreateCustomerInteractionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            Guid? branchId = null;
            if (IsBranchEnabled())
            {
                branchId = await branchAssignments.GetAssignedBranchIdAsync(User, cancellationToken);
                if (branchId is null)
                    return Conflict(new { error = "Your account needs an active branch assignment. Contact your Tenant Admin." });
            }

            var interaction = await createCustomerInteraction.ExecuteAsync(
                new CreateCustomerInteractionCommand(
                    branchId,
                    request.CustomerId,
                    request.Type,
                    request.Subject,
                    request.Details),
                cancellationToken);

            var response = new CustomerInteractionResponse(
                interaction.Id,
                interaction.BranchId,
                interaction.CustomerId,
                interaction.Type,
                interaction.Status,
                interaction.Subject,
                interaction.Details,
                interaction.SubmittedAtUtc);

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerInteractionResponse>>> GetAll(
    CancellationToken cancellationToken)
    {
        Guid? branchId = null;
        if (IsBranchEnabled())
        {
            branchId = await branchAssignments.GetAssignedBranchIdAsync(User, cancellationToken);
            if (branchId is null)
                return Conflict(new { error = "Your account needs an active branch assignment. Contact your Tenant Admin." });
        }

        var interactions =
            await getCustomerInteractions.ExecuteAsync(branchId, cancellationToken);

        var response = interactions
            .Select(interaction => new CustomerInteractionResponse(
                interaction.Id,
                interaction.BranchId,
                interaction.CustomerId,
                interaction.Type,
                interaction.Status,
                interaction.Subject,
                interaction.Details,
                interaction.SubmittedAtUtc))
            .ToList();

        return Ok(response);
    }

    private bool IsBranchEnabled() =>
        string.Equals(currentTenant.CompanySize, "Medium", StringComparison.OrdinalIgnoreCase);
}

public sealed record CreateCustomerInteractionRequest(
    Guid? CustomerId,
    InteractionType Type,
    string Subject,
    string Details);

public sealed record CustomerInteractionResponse(
    Guid Id,
    Guid? BranchId,
    Guid? CustomerId,
    InteractionType Type,
    InteractionStatus Status,
    string Subject,
    string Details,
    DateTime SubmittedAtUtc);
