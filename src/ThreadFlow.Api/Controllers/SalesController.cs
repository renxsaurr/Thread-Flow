using Microsoft.AspNetCore.Mvc;
using ThreadFlow.Api.Modules.Sales;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Route("api/sales")]
public sealed class SalesController(CreateSale createSale, GetSalesCatalog getSalesCatalog) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SaleResponse>> Create(
        [FromBody] CreateSaleRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Lines is null)
            return BadRequest(new { error = "A sale must contain at least one product." });

        try
        {
            var sale = await createSale.ExecuteAsync(
                new CreateSaleCommand(
                    request.BranchId,
                    request.CustomerId,
                    request.Lines
                        .Select(line => new CreateSaleLineCommand(
                            line.ProductId,
                            line.Quantity))
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
    CancellationToken cancellationToken)
    {
        var catalog = await getSalesCatalog.ExecuteAsync(cancellationToken);
        return Ok(catalog);
    }
}

public sealed record CreateSaleRequest(
    Guid BranchId,
    Guid? CustomerId,
    IReadOnlyList<CreateSaleLineRequest>? Lines);

public sealed record CreateSaleLineRequest(Guid ProductId, int Quantity);

public sealed record SaleResponse(
    Guid Id,
    string SaleNumber,
    Guid BranchId,
    Guid? CustomerId,
    DateTime OccurredAtUtc,
    decimal TotalAmount,
    IReadOnlyList<SaleLineResponse> Lines);

public sealed record SaleLineResponse(
    Guid ProductId,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);