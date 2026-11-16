using Microsoft.AspNetCore.Mvc;
using ThreadFlow.Api.Modules.CustomerServiceAndLoyalty;
using ThreadFlow.Domain.Modules.Crm;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Route("api/customer-interactions")]
public sealed class CustomerInteractionsController(
    CreateCustomerInteraction createCustomerInteraction,
    GetCustomerInteractions getCustomerInteractions) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CustomerInteractionResponse>> Create(
        [FromBody] CreateCustomerInteractionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var interaction = await createCustomerInteraction.ExecuteAsync(
                new CreateCustomerInteractionCommand(
                    request.CustomerId,
                    request.Type,
                    request.Subject,
                    request.Details),
                cancellationToken);

            var response = new CustomerInteractionResponse(
                interaction.Id,
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
        var interactions =
            await getCustomerInteractions.ExecuteAsync(cancellationToken);

        var response = interactions
            .Select(interaction => new CustomerInteractionResponse(
                interaction.Id,
                interaction.CustomerId,
                interaction.Type,
                interaction.Status,
                interaction.Subject,
                interaction.Details,
                interaction.SubmittedAtUtc))
            .ToList();

        return Ok(response);
    }
}

public sealed record CreateCustomerInteractionRequest(
    Guid? CustomerId,
    InteractionType Type,
    string Subject,
    string Details);

public sealed record CustomerInteractionResponse(
    Guid Id,
    Guid? CustomerId,
    InteractionType Type,
    InteractionStatus Status,
    string Subject,
    string Details,
    DateTime SubmittedAtUtc);