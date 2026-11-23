using Microsoft.EntityFrameworkCore;
using ThreadFlow.Domain.Modules.Crm;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Modules.CustomerServiceAndLoyalty;

public sealed record CreateCustomerInteractionCommand(
    Guid? BranchId,
    Guid? CustomerId,
    InteractionType Type,
    string Subject,
    string Details);

public sealed class CreateCustomerInteraction(TenantDbContext db) 
{
    public async Task<CustomerInteraction> ExecuteAsync(
        CreateCustomerInteractionCommand command,
        CancellationToken cancellationToken = default)
    {
        var subject = command.Subject.Trim();
        var details = command.Details.Trim();

        if (!Enum.IsDefined(command.Type))
            throw new ArgumentException("Choose Inquiry, Complaint, or Feedback.");

        if (subject.Length == 0 || subject.Length > 160)
            throw new ArgumentException("Subject must be between 1 and 160 characters.");

        if (details.Length == 0 || details.Length > 4000)
            throw new ArgumentException("Details must be between 1 and 4000 characters.");

        if (command.CustomerId is Guid customerId &&
            !await db.Customers.AnyAsync(
                customer => customer.Id == customerId,
                cancellationToken))
        {
            throw new ArgumentException("The selected customer does not exist.");
        }

        var interaction = new CustomerInteraction
        {
            BranchId = command.BranchId,
            CustomerId = command.CustomerId,
            Type = command.Type,
            Subject = subject,
            Details = details
        };

        db.CustomerInteractions.Add(interaction);
        await db.SaveChangesAsync(cancellationToken);

        return interaction;
    }
}
