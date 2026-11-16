using Microsoft.EntityFrameworkCore;
using ThreadFlow.Domain.Modules.Management;
using ThreadFlow.Domain.Modules.Sales;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Modules.Sales;

public sealed record CreateSaleLineCommand(Guid ProductId, int Quantity);

public sealed record CreateSaleCommand(
    Guid BranchId,
    Guid? CustomerId,
    IReadOnlyList<CreateSaleLineCommand> Lines);

public sealed class CreateSale(TenantDbContext db)
{
    public async Task<Sale> ExecuteAsync(
        CreateSaleCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Lines is null || command.Lines.Count == 0)
            throw new ArgumentException("A sale must contain at least one product.");

        if (command.Lines.Any(line => line.Quantity <= 0))
            throw new ArgumentException("Each product quantity must be greater than zero.");

        if (command.Lines.Select(line => line.ProductId).Distinct().Count()
            != command.Lines.Count)
        {
            throw new ArgumentException("Each product can appear only once in a sale.");
        }

        var branchIsActive = await db.Branches.AnyAsync(
            branch => branch.Id == command.BranchId && branch.IsActive,
            cancellationToken);

        if (!branchIsActive)
            throw new ArgumentException("The selected branch does not exist or is inactive.");

        if (command.CustomerId is Guid customerId &&
            !await db.Customers.AnyAsync(
                customer => customer.Id == customerId,
                cancellationToken))
        {
            throw new ArgumentException("The selected customer does not exist.");
        }

        var productIds = command.Lines.Select(line => line.ProductId).ToList();

        var products = await db.Products
            .Where(product => productIds.Contains(product.Id) && product.IsActive)
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        if (products.Count != productIds.Count)
            throw new ArgumentException("One or more products do not exist or are inactive.");

        var saleLines = command.Lines.Select(line =>
        {
            var product = products[line.ProductId];

            return new SaleLine
            {
                ProductId = product.Id,
                Quantity = line.Quantity,
                UnitPrice = product.UnitPrice,
                LineTotal = product.UnitPrice * line.Quantity
            };
        }).ToList();

        var saleNumber =
            $"TF-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..8]}";

        var sale = new Sale
        {
            SaleNumber = saleNumber,
            BranchId = command.BranchId,
            CustomerId = command.CustomerId,
            TotalAmount = saleLines.Sum(line => line.LineTotal),
            Lines = saleLines
        };

        db.Sales.Add(sale);
        await db.SaveChangesAsync(cancellationToken);

        return sale;
    }
}