using Microsoft.EntityFrameworkCore;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Modules.Sales;

public sealed record SalesBranchOption(Guid Id, string Code, string Name);

public sealed record SalesProductOption(
    Guid Id,
    string Sku,
    string Name,
    decimal UnitPrice);

public sealed record SalesCatalogResponse(
    IReadOnlyList<SalesBranchOption> Branches,
    IReadOnlyList<SalesProductOption> Products);

public sealed class GetSalesCatalog(TenantDbContext db)
{
    public async Task<SalesCatalogResponse> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var branches = await db.Branches
            .AsNoTracking()
            .Where(branch => branch.IsActive)
            .OrderBy(branch => branch.Name)
            .Select(branch => new SalesBranchOption(
                branch.Id,
                branch.Code,
                branch.Name))
            .ToListAsync(cancellationToken);

        var products = await db.Products
            .AsNoTracking()
            .Where(product => product.IsActive)
            .OrderBy(product => product.Name)
            .Select(product => new SalesProductOption(
                product.Id,
                product.Sku,
                product.Name,
                product.UnitPrice))
            .ToListAsync(cancellationToken);

        return new SalesCatalogResponse(branches, products);
    }
}