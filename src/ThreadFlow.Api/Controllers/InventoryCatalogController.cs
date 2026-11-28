using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Api.Tenancy;
using ThreadFlow.Domain.Modules.Inventory;
using ThreadFlow.Domain.Modules.Sales;
using ThreadFlow.Infrastructure.Tenancy;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ThreadFlowRoles.InventoryPurchasingStaff)]
[Route("api/inventory")]
public sealed class InventoryCatalogController(
    TenantDbContext tenantDb,
    TenantBranchAssignmentService branchAssignments,
    ICurrentTenantContext currentTenant) : ControllerBase
{
    [HttpGet("branches")]
    public async Task<ActionResult<IReadOnlyList<InventoryBranchDto>>> GetBranches(
        CancellationToken cancellationToken)
    {
        await TenantBranchDefaults.EnsureMainBranchAsync(tenantDb, cancellationToken);
        var branches = tenantDb.Branches.AsNoTracking().Where(branch => branch.IsActive);

        if (IsBranchEnabled())
        {
            var assignedBranchId = await branchAssignments.GetAssignedBranchIdAsync(User, cancellationToken);
            if (assignedBranchId is null)
                return Conflict(new { error = "Your account needs an active branch assignment. Contact your Tenant Admin." });
            branches = branches.Where(branch => branch.Id == assignedBranchId.Value);
        }
        else
        {
            var mainBranchId = await tenantDb.Branches.AsNoTracking()
                .Where(branch => branch.IsActive)
                .OrderBy(branch => branch.Code == "MAIN" ? 0 : 1)
                .ThenBy(branch => branch.Name)
                .Select(branch => branch.Id)
                .FirstOrDefaultAsync(cancellationToken);
            branches = branches.Where(branch => branch.Id == mainBranchId);
        }

        return Ok(await branches.OrderBy(branch => branch.Name)
            .Select(branch => new InventoryBranchDto(branch.Id, branch.Code, branch.Name))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("products")]
    public async Task<ActionResult<IReadOnlyList<InventoryProductDto>>> GetProducts(
        [FromQuery] Guid branchId,
        CancellationToken cancellationToken)
    {
        if (!await IsAllowedBranchAsync(branchId, cancellationToken))
            return Forbid();

        var products = await tenantDb.Products.AsNoTracking()
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Sku)
            .Select(product => new InventoryProductDto(
                product.Id,
                product.Sku,
                product.Name,
                product.Color,
                product.Size,
                product.UnitPrice,
                product.SupplierId,
                null,
                tenantDb.BranchProductStocks.AsNoTracking()
                    .Where(stock => stock.BranchId == branchId && stock.ProductId == product.Id)
                    .Select(stock => (int?)stock.QuantityOnHand)
                    .FirstOrDefault() ?? 0,
                tenantDb.BranchProductStocks.AsNoTracking()
                    .Where(stock => stock.BranchId == branchId && stock.ProductId == product.Id)
                    .Select(stock => (int?)stock.ReorderLevel)
                    .FirstOrDefault() ?? 0,
                product.IsActive))
            .ToListAsync(cancellationToken);

        var productIds = products.Select(product => product.Id).ToArray();
        var supplierNames = await (
            from supplierProduct in tenantDb.SupplierProducts.AsNoTracking()
            join supplier in tenantDb.Suppliers.AsNoTracking()
                on supplierProduct.SupplierId equals supplier.Id
            where productIds.Contains(supplierProduct.ProductId)
            orderby supplier.Name
            select new { supplierProduct.ProductId, supplier.Name })
            .ToListAsync(cancellationToken);
        var namesByProduct = supplierNames
            .GroupBy(item => item.ProductId)
            .ToDictionary(group => group.Key, group => group.Count());
        var result = products.Select(product => product with
        {
            SupplierCount = namesByProduct.GetValueOrDefault(product.Id)
        }).ToList();

        return Ok(result);
    }

    [HttpGet("products/{productId:guid}/suppliers")]
    public async Task<ActionResult<IReadOnlyList<InventoryProductSupplierOfferDto>>> GetProductSupplierOffers(
        Guid productId, CancellationToken cancellationToken)
    {
        if (!await tenantDb.Products.AsNoTracking().AnyAsync(item => item.Id == productId, cancellationToken))
            return NotFound(new { error = "Product was not found." });

        var offers = await (
            from supplierProduct in tenantDb.SupplierProducts.AsNoTracking()
            join supplier in tenantDb.Suppliers.AsNoTracking()
                on supplierProduct.SupplierId equals supplier.Id
            where supplierProduct.ProductId == productId
            orderby supplier.Name
            select new InventoryProductSupplierOfferDto(
                supplier.Id, supplier.Name, supplierProduct.UnitCost, supplierProduct.UpdatedAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(offers);
    }

    [HttpGet("receipts")]
    public async Task<ActionResult<IReadOnlyList<InventoryStockReceiptDto>>> GetReceipts(
        [FromQuery] Guid branchId,
        CancellationToken cancellationToken)
    {
        if (!await IsAllowedBranchAsync(branchId, cancellationToken))
            return Forbid();

        var receipts = await (
            from receipt in tenantDb.InventoryStockReceipts.AsNoTracking()
            join product in tenantDb.Products.AsNoTracking() on receipt.ProductId equals product.Id
            join branch in tenantDb.Branches.AsNoTracking() on receipt.BranchId equals branch.Id
            join supplier in tenantDb.Suppliers.AsNoTracking() on receipt.SupplierId equals supplier.Id into supplierRows
            from supplier in supplierRows.DefaultIfEmpty()
            where receipt.BranchId == branchId
            orderby receipt.ReceivedAtUtc descending
            select new InventoryStockReceiptDto(
                receipt.Id, product.Sku, product.Name, branch.Name,
                supplier == null ? null : supplier.Name, receipt.Quantity,
                receipt.Reference, receipt.ReceivedAtUtc))
            .Take(100)
            .ToListAsync(cancellationToken);

        return Ok(receipts);
    }

    [HttpPost("receipts")]
    public IActionResult ReceiveStockOutsidePurchaseOrder() =>
        Conflict(new
        {
            error = "Supplier stock must be received against a placed purchase order. " +
                "Use Stock Control for opening counts or inventory corrections."
        });

    [HttpPost("products")]
    public async Task<ActionResult<InventoryProductDto>> CreateProduct(
        [FromBody] SaveInventoryProductRequest request,
        CancellationToken cancellationToken)
    {
        var error = await ValidateProductAsync(request, null, cancellationToken);
        if (error is not null)
            return BadRequest(new { error });

        var product = new Product
        {
            Sku = request.Sku.Trim().ToUpperInvariant(),
            Name = request.Name.Trim(),
            Color = request.Color?.Trim() ?? string.Empty,
            Size = request.Size?.Trim() ?? string.Empty,
            UnitPrice = request.UnitPrice,
            SupplierId = request.SupplierId,
            IsActive = true
        };

        tenantDb.Products.Add(product);
        if (product.SupplierId is Guid preferredSupplierId)
        {
            tenantDb.SupplierProducts.Add(new SupplierProduct
            {
                SupplierId = preferredSupplierId,
                ProductId = product.Id,
                UnitCost = 0,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        try
        {
            await tenantDb.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "That product SKU is already in use." });
        }

        var supplierName = product.SupplierId is Guid supplierId
            ? await tenantDb.Suppliers.Where(item => item.Id == supplierId)
                .Select(item => item.Name).SingleOrDefaultAsync(cancellationToken)
            : null;

        var result = ToDto(product, supplierName);
        return CreatedAtAction(nameof(GetProducts), result);
    }

    [HttpPut("products/{id:guid}")]
    public async Task<ActionResult<InventoryProductDto>> UpdateProduct(
        Guid id,
        [FromBody] SaveInventoryProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await tenantDb.Products.SingleOrDefaultAsync(
            item => item.Id == id, cancellationToken);
        if (product is null)
            return NotFound(new { error = "Product was not found." });

        var error = await ValidateProductAsync(request, product.SupplierId, cancellationToken);
        if (error is not null)
            return BadRequest(new { error });

        product.Sku = request.Sku.Trim().ToUpperInvariant();
        product.Name = request.Name.Trim();
        product.Color = request.Color?.Trim() ?? string.Empty;
        product.Size = request.Size?.Trim() ?? string.Empty;
        product.UnitPrice = request.UnitPrice;
        product.SupplierId = request.SupplierId;
        if (product.SupplierId is Guid preferredSupplierId &&
            !await tenantDb.SupplierProducts.AnyAsync(
                item => item.SupplierId == preferredSupplierId && item.ProductId == product.Id,
                cancellationToken))
        {
            tenantDb.SupplierProducts.Add(new SupplierProduct
            {
                SupplierId = preferredSupplierId,
                ProductId = product.Id,
                UnitCost = 0,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        try
        {
            await tenantDb.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "That product SKU is already in use." });
        }

        var supplierName = product.SupplierId is Guid supplierId
            ? await tenantDb.Suppliers.Where(item => item.Id == supplierId)
                .Select(item => item.Name).SingleOrDefaultAsync(cancellationToken)
            : null;

        return Ok(ToDto(product, supplierName));
    }

    [HttpPatch("products/{id:guid}/status")]
    public async Task<IActionResult> SetProductStatus(
        Guid id,
        [FromBody] SetCatalogStatusRequest request,
        CancellationToken cancellationToken)
    {
        var product = await tenantDb.Products.SingleOrDefaultAsync(
            item => item.Id == id, cancellationToken);
        if (product is null)
            return NotFound(new { error = "Product was not found." });

        product.IsActive = request.IsActive;
        await tenantDb.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("suppliers")]
    public async Task<ActionResult<IReadOnlyList<InventorySupplierDto>>> GetSuppliers(
        CancellationToken cancellationToken)
    {
        var suppliers = await tenantDb.Suppliers
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new InventorySupplierDto(
                item.Id, item.Name, item.ContactName, item.Email, item.Phone, item.Address, item.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(suppliers);
    }

    [HttpPost("suppliers")]
    public async Task<ActionResult<InventorySupplierDto>> CreateSupplier(
        [FromBody] SaveInventorySupplierRequest request,
        CancellationToken cancellationToken)
    {
        var error = ValidateSupplier(request);
        if (error is not null)
            return BadRequest(new { error });

        var supplier = new Supplier
        {
            Name = request.Name.Trim(),
            ContactName = request.ContactName?.Trim() ?? string.Empty,
            Email = request.Email?.Trim() ?? string.Empty,
            Phone = request.Phone?.Trim() ?? string.Empty,
            Address = request.Address?.Trim() ?? string.Empty,
            IsActive = true
        };

        tenantDb.Suppliers.Add(supplier);
        await tenantDb.SaveChangesAsync(cancellationToken);
        var result = ToDto(supplier);
        return CreatedAtAction(nameof(GetSuppliers), result);
    }

    [HttpPut("suppliers/{id:guid}")]
    public async Task<ActionResult<InventorySupplierDto>> UpdateSupplier(
        Guid id,
        [FromBody] SaveInventorySupplierRequest request,
        CancellationToken cancellationToken)
    {
        var supplier = await tenantDb.Suppliers.SingleOrDefaultAsync(
            item => item.Id == id, cancellationToken);
        if (supplier is null)
            return NotFound(new { error = "Supplier was not found." });

        var error = ValidateSupplier(request);
        if (error is not null)
            return BadRequest(new { error });

        supplier.Name = request.Name.Trim();
        supplier.ContactName = request.ContactName?.Trim() ?? string.Empty;
        supplier.Email = request.Email?.Trim() ?? string.Empty;
        supplier.Phone = request.Phone?.Trim() ?? string.Empty;
        supplier.Address = request.Address?.Trim() ?? string.Empty;
        await tenantDb.SaveChangesAsync(cancellationToken);
        return Ok(ToDto(supplier));
    }

    [HttpPatch("suppliers/{id:guid}/status")]
    public async Task<IActionResult> SetSupplierStatus(
        Guid id,
        [FromBody] SetCatalogStatusRequest request,
        CancellationToken cancellationToken)
    {
        var supplier = await tenantDb.Suppliers.SingleOrDefaultAsync(
            item => item.Id == id, cancellationToken);
        if (supplier is null)
            return NotFound(new { error = "Supplier was not found." });

        supplier.IsActive = request.IsActive;
        await tenantDb.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("suppliers/{supplierId:guid}/products")]
    public async Task<ActionResult<IReadOnlyList<SupplierProductDto>>> GetSupplierProducts(
        Guid supplierId, CancellationToken cancellationToken)
    {
        if (!await tenantDb.Suppliers.AnyAsync(item => item.Id == supplierId, cancellationToken))
            return NotFound(new { error = "Supplier was not found." });

        var products = await (
            from supplierProduct in tenantDb.SupplierProducts.AsNoTracking()
            join product in tenantDb.Products.AsNoTracking()
                on supplierProduct.ProductId equals product.Id
            where supplierProduct.SupplierId == supplierId
            orderby product.Name, product.Sku
            select new SupplierProductDto(
                product.Id, product.Sku, product.Name, product.Color, product.Size,
                supplierProduct.UnitCost, supplierProduct.UpdatedAtUtc, product.IsActive))
            .ToListAsync(cancellationToken);

        return Ok(products);
    }

    [HttpPut("suppliers/{supplierId:guid}/products")]
    public async Task<IActionResult> ReplaceSupplierProducts(
        Guid supplierId, [FromBody] ReplaceSupplierProductsRequest request,
        CancellationToken cancellationToken)
    {
        if (!await tenantDb.Suppliers.AnyAsync(
                item => item.Id == supplierId && item.IsActive, cancellationToken))
            return BadRequest(new { error = "Choose an active supplier." });
        if (request.Products.Count > 1000)
            return BadRequest(new { error = "A supplier can have up to 1,000 linked products." });
        if (request.Products.GroupBy(item => item.ProductId).Any(group => group.Count() > 1))
            return BadRequest(new { error = "A product can appear only once for this supplier." });
        if (request.Products.Any(item => item.UnitCost < 0 || item.UnitCost > 9999999999999999.99m))
            return BadRequest(new { error = "Unit costs must be within the supported price range." });

        var productIds = request.Products.Select(item => item.ProductId).ToArray();
        var activeProductIds = await tenantDb.Products.AsNoTracking()
            .Where(item => productIds.Contains(item.Id) && item.IsActive)
            .Select(item => item.Id)
            .ToListAsync(cancellationToken);
        if (activeProductIds.Count != productIds.Length)
            return BadRequest(new { error = "Every linked item must be an active catalog product." });

        await using var transaction = await tenantDb.Database.BeginTransactionAsync(cancellationToken);
        var current = await tenantDb.SupplierProducts
            .Where(item => item.SupplierId == supplierId)
            .ToListAsync(cancellationToken);
        var requestedByProduct = request.Products.ToDictionary(item => item.ProductId);

        var removed = current.Where(item => !requestedByProduct.ContainsKey(item.ProductId)).ToList();
        tenantDb.SupplierProducts.RemoveRange(removed);
        foreach (var item in current.Where(item => requestedByProduct.ContainsKey(item.ProductId)))
        {
            item.UnitCost = requestedByProduct[item.ProductId].UnitCost;
            item.UpdatedAtUtc = DateTime.UtcNow;
        }
        var currentIds = current.Select(item => item.ProductId).ToHashSet();
        foreach (var item in request.Products.Where(item => !currentIds.Contains(item.ProductId)))
        {
            tenantDb.SupplierProducts.Add(new SupplierProduct
            {
                SupplierId = supplierId,
                ProductId = item.ProductId,
                UnitCost = item.UnitCost,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        // Keep the catalog's existing preferred-supplier field in sync for the Products table.
        var affectedProductIds = request.Products.Select(item => item.ProductId)
            .Concat(removed.Select(item => item.ProductId)).Distinct().ToArray();
        var affectedProducts = await tenantDb.Products
            .Where(item => affectedProductIds.Contains(item.Id))
            .ToListAsync(cancellationToken);
        foreach (var product in affectedProducts)
        {
            var remainsWithSelectedSupplier = requestedByProduct.ContainsKey(product.Id);
            if (remainsWithSelectedSupplier && product.SupplierId is null)
            {
                product.SupplierId = supplierId;
            }
            else if (!remainsWithSelectedSupplier && product.SupplierId == supplierId)
            {
                product.SupplierId = await tenantDb.SupplierProducts
                    .Where(item => item.ProductId == product.Id && item.SupplierId != supplierId)
                    .Select(item => (Guid?)item.SupplierId)
                    .FirstOrDefaultAsync(cancellationToken);
            }
        }

        await tenantDb.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    private async Task<string?> ValidateProductAsync(
        SaveInventoryProductRequest request,
        Guid? unchangedSupplierId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Sku) || request.Sku.Trim().Length > 48)
            return "SKU is required and must be 48 characters or fewer.";
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 160)
            return "Product name is required and must be 160 characters or fewer.";
        if (request.Color?.Trim().Length > 48)
            return "Color must be 48 characters or fewer.";
        if (request.Size?.Trim().Length > 32)
            return "Size must be 32 characters or fewer.";
        if (request.UnitPrice < 0)
            return "Unit price cannot be negative.";
        if (request.SupplierId is Guid supplierId &&
            !await tenantDb.Suppliers.AnyAsync(
                item => item.Id == supplierId &&
                    (item.IsActive || item.Id == unchangedSupplierId),
                cancellationToken))
            return "Choose an active supplier.";

        return null;
    }

    private static string? ValidateSupplier(SaveInventorySupplierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 160)
            return "Supplier name is required and must be 160 characters or fewer.";
        if (request.ContactName?.Trim().Length > 160)
            return "Contact name must be 160 characters or fewer.";
        if (request.Email?.Trim().Length > 254)
            return "Email must be 254 characters or fewer.";
        if (request.Phone?.Trim().Length > 32)
            return "Phone must be 32 characters or fewer.";
        if (request.Address?.Trim().Length > 300)
            return "Address must be 300 characters or fewer.";
        return null;
    }

    private bool IsBranchEnabled() =>
        string.Equals(currentTenant.CompanySize, "Medium", StringComparison.OrdinalIgnoreCase);

    private async Task<bool> IsAllowedBranchAsync(Guid branchId, CancellationToken cancellationToken)
    {
        if (branchId == Guid.Empty)
            return false;

        if (IsBranchEnabled())
        {
            var assignedBranchId = await branchAssignments.GetAssignedBranchIdAsync(User, cancellationToken);
            return assignedBranchId == branchId;
        }

        await TenantBranchDefaults.EnsureMainBranchAsync(tenantDb, cancellationToken);
        var mainBranchId = await tenantDb.Branches.AsNoTracking()
            .Where(branch => branch.IsActive)
            .OrderBy(branch => branch.Code == "MAIN" ? 0 : 1)
            .ThenBy(branch => branch.Name)
            .Select(branch => (Guid?)branch.Id)
            .FirstOrDefaultAsync(cancellationToken);
        return mainBranchId == branchId;
    }

    private static InventoryProductDto ToDto(Product product, string? supplierName) => new(
        product.Id, product.Sku, product.Name, product.Color, product.Size,
        product.UnitPrice, product.SupplierId, supplierName, 0, 0, product.IsActive);

    private static InventorySupplierDto ToDto(Supplier supplier) => new(
        supplier.Id, supplier.Name, supplier.ContactName, supplier.Email,
        supplier.Phone, supplier.Address, supplier.IsActive);
}

public sealed record InventoryProductDto(
    Guid Id, string Sku, string Name, string Color, string Size,
    decimal UnitPrice, Guid? SupplierId, string? SupplierName, int QuantityOnHand, int ReorderLevel, bool IsActive,
    int SupplierCount = 0);

public sealed record InventoryProductSupplierOfferDto(
    Guid SupplierId, string SupplierName, decimal UnitCost, DateTime UpdatedAtUtc);

public sealed record InventoryBranchDto(Guid Id, string Code, string Name);

public sealed record InventoryStockReceiptDto(
    Guid Id, string Sku, string ProductName, string BranchName,
    string? SupplierName, int Quantity, string Reference, DateTime ReceivedAtUtc);

public sealed record SupplierProductDto(
    Guid ProductId, string Sku, string ProductName, string Color, string Size,
    decimal UnitCost, DateTime UpdatedAtUtc, bool IsActive);

public sealed record SupplierProductLineRequest(Guid ProductId, decimal UnitCost);

public sealed record ReplaceSupplierProductsRequest(List<SupplierProductLineRequest> Products);

public sealed record SaveInventoryProductRequest(
    string Sku, string Name, string? Color, string? Size, decimal UnitPrice, Guid? SupplierId);

public sealed record InventorySupplierDto(
    Guid Id, string Name, string ContactName, string Email, string Phone, string Address, bool IsActive);

public sealed record SaveInventorySupplierRequest(
    string Name, string? ContactName, string? Email, string? Phone, string? Address);

public sealed record SetCatalogStatusRequest(bool IsActive);
