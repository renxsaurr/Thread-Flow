using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Tenancy;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Modules.Sales;

public sealed record SalesBranchOption(Guid Id, string Code, string Name);

public sealed record SalesProductOption(
    Guid Id,
    string Sku,
    string Name,
    string Color,
    string Size,
    decimal UnitPrice,
    int QuantityOnHand);

public sealed record SalesCatalogResponse(
    IReadOnlyList<SalesBranchOption> Branches,
    IReadOnlyList<SalesProductOption> Products);

public sealed class GetSalesCatalog(TenantDbContext db)
{
    public async Task<SalesCatalogResponse> ExecuteAsync(
        Guid? branchId,
        string? searchTerm,
        CancellationToken cancellationToken = default)
    {
        await TenantBranchDefaults.EnsureMainBranchAsync(db, cancellationToken);

        var branches = await db.Branches
            .AsNoTracking()
            .Where(branch => branch.IsActive)
            .OrderBy(branch => branch.Name)
            .Select(branch => new SalesBranchOption(
                branch.Id,
                branch.Code,
                branch.Name))
            .ToListAsync(cancellationToken);

        IReadOnlyList<SalesProductOption> products =
            Array.Empty<SalesProductOption>();

        if (branchId is Guid selectedBranchId)
        {
            var branchIsActive = await db.Branches.AnyAsync(
                branch => branch.Id == selectedBranchId && branch.IsActive,
                cancellationToken);

            if (!branchIsActive)
                throw new ArgumentException("The selected branch does not exist or is inactive.");

            var productQuery =
                from product in db.Products.AsNoTracking()
                where product.IsActive
                join stock in db.BranchProductStocks
                        .AsNoTracking()
                        .Where(stock => stock.BranchId == selectedBranchId)
                    on product.Id equals stock.ProductId into stockRows
                from stock in stockRows.DefaultIfEmpty()
                select new { Product = product, Stock = stock };

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();

                productQuery = productQuery.Where(row =>
                    row.Product.Sku.Contains(term) ||
                    row.Product.Name.Contains(term) ||
                    row.Product.Color.Contains(term) ||
                    row.Product.Size.Contains(term));
            }

            products = await productQuery
                .OrderBy(row => row.Product.Name)
                .Select(row => new SalesProductOption(
                    row.Product.Id,
                    row.Product.Sku,
                    row.Product.Name,
                    row.Product.Color,
                    row.Product.Size,
                    row.Product.UnitPrice,
                    row.Stock == null ? 0 : row.Stock.QuantityOnHand))
                .ToListAsync(cancellationToken);
        }

        return new SalesCatalogResponse(branches, products);
    }
}
