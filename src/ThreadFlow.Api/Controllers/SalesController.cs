using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Api.Modules.Sales;
using ThreadFlow.Api.Tenancy;
using ThreadFlow.Domain.Modules.Sales;
using ThreadFlow.Infrastructure.Tenancy;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ThreadFlowRoles.SalesClerk)]
[Route("api/sales")]
public sealed class SalesController(
    CreateSale createSale,
    GetSalesCatalog getSalesCatalog,
    TenantBranchAssignmentService branchAssignments,
    ICurrentTenantContext currentTenant) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SaleResponse>> Create(
        [FromBody] CreateSaleRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Lines is null)
            return BadRequest(new { error = "A sale must contain at least one product." });

        if (request.Payments is null)
            return BadRequest(new { error = "A sale must contain at least one payment." });

        var branchId = request.BranchId;
        if (IsBranchEnabled())
        {
            var assignedBranchId = await branchAssignments.GetAssignedBranchIdAsync(User, cancellationToken);
            if (assignedBranchId is null)
                return Conflict(new { error = "Your account needs an active branch assignment. Contact your Tenant Admin." });
            if (request.BranchId != assignedBranchId.Value)
                return Forbid();
            branchId = assignedBranchId.Value;
        }

        try
        {
            var sale = await createSale.ExecuteAsync(
                new CreateSaleCommand(
                    branchId,
                    request.CustomerId,
                    request.Lines
                        .Select(line => new CreateSaleLineCommand(
                            line.ProductId,
                            line.Quantity))
                        .ToList(),
                    request.Payments
                        .Select(payment => new CreateSalePaymentCommand(
                            payment.Method,
                            payment.Amount))
                        .ToList()),
                cancellationToken);

            var response = new SaleResponse(
                sale.Id,
                sale.SaleNumber,
                sale.BranchId,
                sale.CustomerId,
                sale.OccurredAtUtc,
                sale.TotalAmount,
                sale.Lines
                    .Select(line => new SaleLineResponse(
                        line.ProductId,
                        line.Quantity,
                        line.UnitPrice,
                        line.LineTotal))
                    .ToList(),
                sale.Payments
                    .Select(payment => new SalePaymentResponse(
                        payment.Method,
                        payment.Amount,
                        payment.PaidAtUtc))
                    .ToList());

            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("catalog")]
    public async Task<ActionResult<SalesCatalogResponse>> GetCatalog(
        [FromQuery] Guid? branchId,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        try
        {
            var branchToLoad = branchId;
            Guid? assignedBranchId = null;
            if (IsBranchEnabled())
            {
                assignedBranchId = await branchAssignments.GetAssignedBranchIdAsync(User, cancellationToken);
                if (assignedBranchId is null)
                    return Conflict(new { error = "Your account needs an active branch assignment. Contact your Tenant Admin." });
                if (branchId is Guid requestedBranchId && requestedBranchId != assignedBranchId.Value)
                    return Forbid();
                branchToLoad = assignedBranchId;
            }

            var catalog = await getSalesCatalog.ExecuteAsync(
                branchToLoad,
                searchTerm,
                cancellationToken);

            if (assignedBranchId is Guid employeeBranchId)
            {
                catalog = catalog with
                {
                    Branches = catalog.Branches
                        .Where(branch => branch.Id == employeeBranchId)
                        .ToArray()
                };
            }

            return Ok(catalog);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    private bool IsBranchEnabled() =>
        string.Equals(currentTenant.CompanySize, "Medium", StringComparison.OrdinalIgnoreCase);
}

public sealed record CreateSaleRequest(
    Guid BranchId,
    Guid? CustomerId,
    IReadOnlyList<CreateSaleLineRequest>? Lines,
    IReadOnlyList<CreateSalePaymentRequest>? Payments);

public sealed record CreateSaleLineRequest(Guid ProductId, int Quantity);
public sealed record CreateSalePaymentRequest(PaymentMethod Method, decimal Amount);

public sealed record SaleResponse(
    Guid Id,
    string SaleNumber,
    Guid BranchId,
    Guid? CustomerId,
    DateTime OccurredAtUtc,
    decimal TotalAmount,
    IReadOnlyList<SaleLineResponse> Lines,
    IReadOnlyList<SalePaymentResponse> Payments);

public sealed record SaleLineResponse(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public sealed record SalePaymentResponse(
    PaymentMethod Method,
    decimal Amount,
    DateTime PaidAtUtc);
