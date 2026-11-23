using Microsoft.EntityFrameworkCore;
using ThreadFlow.Domain.Modules.Management;
using ThreadFlow.Domain.Modules.Sales;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Modules.Sales;

public sealed record CreateSaleLineCommand(Guid ProductId, int Quantity);
public sealed record CreateSalePaymentCommand(PaymentMethod Method, decimal Amount);

public sealed record CreateSaleCommand(
    Guid BranchId,
    Guid? CustomerId,
    IReadOnlyList<CreateSaleLineCommand> Lines,
    IReadOnlyList<CreateSalePaymentCommand> Payments);

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

        if (command.Payments is null || command.Payments.Count == 0)
            throw new ArgumentException("A sale must contain at least one payment.");

        if (command.Payments.Any(payment => payment.Amount <= 0))
            throw new ArgumentException("Each payment amount must be greater than zero.");

        if (command.Payments.Any(payment =>
                !Enum.IsDefined(typeof(PaymentMethod), payment.Method)))
        {
            throw new ArgumentException("One or more payment methods are invalid.");
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

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        foreach (var line in command.Lines.OrderBy(line => line.ProductId))
        {
            var rowsUpdated = await db.BranchProductStocks
                .Where(stock =>
                    stock.BranchId == command.BranchId &&
                    stock.ProductId == line.ProductId &&
                    stock.QuantityOnHand >= line.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        stock => stock.QuantityOnHand,
                        stock => stock.QuantityOnHand - line.Quantity),
                    cancellationToken);

            if (rowsUpdated != 1)
            {
                var sku = products[line.ProductId].Sku;
                throw new ArgumentException(
                    $"Not enough stock for {sku} at the selected branch.");
            }
        }

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
            Lines = saleLines,
            Payments = command.Payments.Select(payment => new SalePayment
            {
                Method = payment.Method,
                Amount = payment.Amount
            }).ToList()
        };

        if (sale.Payments.Sum(payment => payment.Amount) != sale.TotalAmount)
        {
            throw new ArgumentException(
                "Payment amounts must add up to the sale total.");
        }

        db.Sales.Add(sale);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return sale;
    }
}
