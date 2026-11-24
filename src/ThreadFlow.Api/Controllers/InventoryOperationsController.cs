using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Api.Tenancy;
using ThreadFlow.Domain.Modules.Inventory;
using ThreadFlow.Infrastructure.Tenancy;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ThreadFlowRoles.InventoryPurchasingStaff)]
[Route("api/inventory")]
public sealed class InventoryOperationsController(
    TenantDbContext db,
    TenantBranchAssignmentService branchAssignments,
    ICurrentTenantContext currentTenant) : ControllerBase
{
    [HttpGet("purchase-orders")]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderDto>>> GetPurchaseOrders(
        [FromQuery] Guid branchId, CancellationToken ct)
    {
        if (!await IsAllowedBranchAsync(branchId, ct)) return Forbid();
        return Ok(await db.PurchaseOrders.AsNoTracking()
            .Where(x => x.BranchId == branchId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new PurchaseOrderDto(x.Id, x.Number, x.BranchId, x.SupplierId,
                db.Suppliers.Where(s => s.Id == x.SupplierId).Select(s => s.Name).FirstOrDefault() ?? "(supplier removed)",
                x.Status, x.CreatedAtUtc, x.ExpectedAtUtc, x.OrderedAtUtc, x.Notes,
                x.Lines.Select(line => new PurchaseOrderLineDto(line.Id, line.ProductId,
                    db.Products.Where(p => p.Id == line.ProductId).Select(p => p.Sku).FirstOrDefault() ?? "",
                    db.Products.Where(p => p.Id == line.ProductId).Select(p => p.Name).FirstOrDefault() ?? "",
                    line.QuantityOrdered, line.QuantityReceived, line.UnitCost)).ToList()))
            .ToListAsync(ct));
    }

    [HttpGet("transfer-destinations")]
    public async Task<ActionResult<IReadOnlyList<InventoryBranchDto>>> GetTransferDestinations(CancellationToken ct)
    {
        if (IsBranchEnabled())
        {
            return Ok(await db.Branches.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.Name)
                .Select(b => new InventoryBranchDto(b.Id, b.Code, b.Name)).ToListAsync(ct));
        }
        await TenantBranchDefaults.EnsureMainBranchAsync(db, ct);
        return Ok(await db.Branches.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.Name)
            .Select(b => new InventoryBranchDto(b.Id, b.Code, b.Name)).ToListAsync(ct));
    }

    [HttpPost("purchase-orders")]
    public async Task<ActionResult<PurchaseOrderDto>> CreatePurchaseOrder(
        [FromBody] SavePurchaseOrderRequest request, CancellationToken ct)
    {
        var error = await ValidatePurchaseOrderAsync(request, ct);
        if (error is not null) return BadRequest(new { error });
        var now = DateTime.UtcNow;
        var order = new PurchaseOrder
        {
            Number = $"PO-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..40].ToUpperInvariant(),
            SupplierId = request.SupplierId,
            BranchId = request.BranchId,
            ExpectedAtUtc = request.ExpectedAtUtc,
            Notes = request.Notes?.Trim() ?? string.Empty,
            CreatedByUserId = UserId(),
            Lines = request.Lines.Select(x => new PurchaseOrderLine
            {
                ProductId = x.ProductId,
                QuantityOrdered = x.Quantity,
                UnitCost = x.UnitCost
            }).ToList()
        };
        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync(ct);
        return Ok(await ToDtoAsync(order.Id, ct));
    }

    [HttpPut("purchase-orders/{id:guid}")]
    public async Task<ActionResult<PurchaseOrderDto>> UpdatePurchaseOrder(
        Guid id, [FromBody] SavePurchaseOrderRequest request, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return NotFound(new { error = "Purchase order not found." });
        if (order.Status != "Draft") return Conflict(new { error = "Only draft purchase orders can be edited." });
        if (order.BranchId != request.BranchId || !await IsAllowedBranchAsync(order.BranchId, ct)) return Forbid();
        var error = await ValidatePurchaseOrderAsync(request, ct);
        if (error is not null) return BadRequest(new { error });
        order.SupplierId = request.SupplierId;
        order.ExpectedAtUtc = request.ExpectedAtUtc;
        order.Notes = request.Notes?.Trim() ?? string.Empty;
        db.PurchaseOrderLines.RemoveRange(order.Lines);
        order.Lines.Clear();
        db.PurchaseOrderLines.AddRange(request.Lines.Select(x => new PurchaseOrderLine
        {
            PurchaseOrderId = order.Id, ProductId = x.ProductId,
            QuantityOrdered = x.Quantity, UnitCost = x.UnitCost
        }));
        await db.SaveChangesAsync(ct);
        return Ok(await ToDtoAsync(order.Id, ct));
    }

    [HttpPost("purchase-orders/{id:guid}/place")]
    public async Task<IActionResult> PlacePurchaseOrder(Guid id, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return NotFound(new { error = "Purchase order not found." });
        if (!await IsAllowedBranchAsync(order.BranchId, ct)) return Forbid();
        if (order.Status != "Draft") return Conflict(new { error = "Only a draft can be placed." });
        order.Status = "Ordered";
        order.OrderedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("purchase-orders/{id:guid}/cancel")]
    public async Task<IActionResult> CancelPurchaseOrder(Guid id, CancellationToken ct)
    {
        var order = await db.PurchaseOrders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return NotFound(new { error = "Purchase order not found." });
        if (!await IsAllowedBranchAsync(order.BranchId, ct)) return Forbid();
        if (order.Status is "Received" or "Cancelled") return Conflict(new { error = "This purchase order cannot be cancelled." });
        order.Status = "Cancelled";
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("purchase-orders/{id:guid}/receive")]
    public async Task<ActionResult> ReceivePurchaseOrder(
        Guid id, [FromBody] ReceivePurchaseOrderRequest request, CancellationToken ct)
    {
        if (request.Reference?.Trim().Length > 80) return BadRequest(new { error = "Reference must be 80 characters or fewer." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var order = await db.PurchaseOrders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is null) return NotFound(new { error = "Purchase order not found." });
        if (!await IsAllowedBranchAsync(order.BranchId, ct)) return Forbid();
        if (order.Status is not ("Ordered" or "PartiallyReceived")) return Conflict(new { error = "Place the purchase order before receiving it." });
        if (request.Lines.Count == 0 || request.Lines.Any(x => x.Quantity <= 0)) return BadRequest(new { error = "Enter a positive received quantity for at least one line." });
        if (request.Lines.GroupBy(x => x.PurchaseOrderLineId).Any(g => g.Count() > 1)) return BadRequest(new { error = "Each purchase order line can appear only once per receipt." });
        foreach (var received in request.Lines)
        {
            var line = order.Lines.SingleOrDefault(x => x.Id == received.PurchaseOrderLineId);
            if (line is null || received.Quantity > line.QuantityOrdered - line.QuantityReceived)
                return BadRequest(new { error = "A received quantity exceeds the remaining quantity on the purchase order." });
            line.QuantityReceived += received.Quantity;
            await IncreaseStockAsync(order.BranchId, line.ProductId, received.Quantity, ct);
            db.InventoryStockReceipts.Add(new InventoryStockReceipt
            {
                BranchId = order.BranchId, ProductId = line.ProductId,
                SupplierId = order.SupplierId, ReceivedByUserId = UserId(), Quantity = received.Quantity,
                Reference = $"{order.Number} {request.Reference?.Trim()}".Trim()[..Math.Min(80, $"{order.Number} {request.Reference?.Trim()}".Trim().Length)],
                ReceivedAtUtc = DateTime.UtcNow
            });
            db.InventoryStockMovements.Add(new InventoryStockMovement
            {
                Type = "Purchase order receipt", BranchId = order.BranchId, ProductId = line.ProductId,
                SupplierId = order.SupplierId, Quantity = received.Quantity, QuantityChange = received.Quantity,
                Reference = $"{order.Number} {request.Reference?.Trim()}".Trim()[..Math.Min(80, $"{order.Number} {request.Reference?.Trim()}".Trim().Length)],
                Notes = "Goods received against purchase order.", PerformedByUserId = UserId(), OccurredAtUtc = DateTime.UtcNow
            });
        }
        order.Status = order.Lines.All(x => x.QuantityReceived == x.QuantityOrdered) ? "Received" : "PartiallyReceived";
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(await ToDtoAsync(order.Id, ct));
    }

    [HttpGet("low-stock")]
    public async Task<ActionResult<IReadOnlyList<LowStockDto>>> GetLowStock(
        [FromQuery] Guid branchId, CancellationToken ct)
    {
        if (!await IsAllowedBranchAsync(branchId, ct)) return Forbid();
        return Ok(await (from stock in db.BranchProductStocks.AsNoTracking()
                         join product in db.Products.AsNoTracking() on stock.ProductId equals product.Id
                         where stock.BranchId == branchId && product.IsActive && stock.ReorderLevel > 0 && stock.QuantityOnHand <= stock.ReorderLevel
                         orderby stock.QuantityOnHand
                         select new LowStockDto(stock.ProductId, product.Sku, product.Name, stock.QuantityOnHand, stock.ReorderLevel))
            .ToListAsync(ct));
    }

    [HttpPut("stock/{branchId:guid}/{productId:guid}/reorder-level")]
    public async Task<IActionResult> SetReorderLevel(Guid branchId, Guid productId, [FromBody] SetReorderLevelRequest request, CancellationToken ct)
    {
        if (!await IsAllowedBranchAsync(branchId, ct)) return Forbid();
        if (request.ReorderLevel < 0) return BadRequest(new { error = "Reorder level cannot be negative." });
        if (!await db.Products.AnyAsync(x => x.Id == productId, ct)) return NotFound(new { error = "Product not found." });
        var stock = await GetOrCreateStockAsync(branchId, productId, ct);
        stock.ReorderLevel = request.ReorderLevel;
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("stock/adjustments")]
    public async Task<IActionResult> AdjustStock([FromBody] AdjustInventoryStockRequest request, CancellationToken ct)
    {
        if (!await IsAllowedBranchAsync(request.BranchId, ct)) return Forbid();
        if (!await db.Products.AnyAsync(x => x.Id == request.ProductId, ct)) return BadRequest(new { error = "Choose a product that exists." });
        if (request.QuantityChange == 0) return BadRequest(new { error = "Adjustment must change stock by at least one unit." });
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500) return BadRequest(new { error = "A reason of up to 500 characters is required." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var stock = await GetOrCreateStockAsync(request.BranchId, request.ProductId, ct);
        var next = (long)stock.QuantityOnHand + request.QuantityChange;
        if (next < 0 || next > int.MaxValue) return BadRequest(new { error = "Adjustment would result in an invalid stock level." });
        stock.QuantityOnHand = (int)next;
        AddMovement(request.BranchId, request.ProductId, request.QuantityChange,
            request.QuantityChange < 0 ? "Damaged/Write-off" : "Adjustment", request.Reference, request.Reason);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return NoContent();
    }

    [HttpPost("stock/counts")]
    public async Task<IActionResult> RecordStockCount([FromBody] RecordStockCountRequest request, CancellationToken ct)
    {
        if (!await IsAllowedBranchAsync(request.BranchId, ct)) return Forbid();
        if (!await db.Products.AnyAsync(x => x.Id == request.ProductId, ct)) return BadRequest(new { error = "Choose a product that exists." });
        if (request.CountedQuantity < 0) return BadRequest(new { error = "Counted quantity cannot be negative." });
        if (request.Notes?.Trim().Length > 500) return BadRequest(new { error = "Notes must be 500 characters or fewer." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var stock = await GetOrCreateStockAsync(request.BranchId, request.ProductId, ct);
        var delta = request.CountedQuantity - stock.QuantityOnHand;
        stock.QuantityOnHand = request.CountedQuantity;
        AddMovement(request.BranchId, request.ProductId, delta, "Physical count", request.Reference,
            string.IsNullOrWhiteSpace(request.Notes) ? $"Counted {request.CountedQuantity} units." : request.Notes);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return NoContent();
    }

    [HttpPost("stock/returns")]
    public async Task<IActionResult> ReturnToSupplier([FromBody] ReturnInventoryStockRequest request, CancellationToken ct)
    {
        if (!await IsAllowedBranchAsync(request.BranchId, ct)) return Forbid();
        if (!await db.Products.AnyAsync(x => x.Id == request.ProductId, ct)) return BadRequest(new { error = "Choose a product that exists." });
        if (request.Quantity <= 0) return BadRequest(new { error = "Return quantity must be greater than zero." });
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 500) return BadRequest(new { error = "A return reason is required." });
        if (request.Reference?.Trim().Length > 80) return BadRequest(new { error = "Reference must be 80 characters or fewer." });
        if (!await db.Suppliers.AnyAsync(x => x.Id == request.SupplierId, ct)) return BadRequest(new { error = "Choose a valid supplier." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var stock = await GetOrCreateStockAsync(request.BranchId, request.ProductId, ct);
        if (stock.QuantityOnHand < request.Quantity) return BadRequest(new { error = "There is not enough stock at this branch to return that quantity." });
        stock.QuantityOnHand -= request.Quantity;
        AddMovement(request.BranchId, request.ProductId, -request.Quantity, "Supplier return", request.Reference, request.Reason, request.SupplierId);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return NoContent();
    }

    [HttpPost("stock/transfers")]
    public async Task<IActionResult> TransferStock([FromBody] TransferInventoryStockRequest request, CancellationToken ct)
    {
        if (!await IsAllowedBranchAsync(request.FromBranchId, ct)) return Forbid();
        if (request.FromBranchId == request.ToBranchId) return BadRequest(new { error = "Choose a different destination branch." });
        if (!await db.Branches.AnyAsync(x => x.Id == request.ToBranchId && x.IsActive, ct)) return BadRequest(new { error = "Destination branch not found or inactive." });
        if (request.Quantity <= 0) return BadRequest(new { error = "Transfer quantity must be greater than zero." });
        if (request.Reference?.Trim().Length > 80 || request.Notes?.Trim().Length > 500) return BadRequest(new { error = "Reference or notes exceed the allowed length." });
        if (!await db.Products.AnyAsync(x => x.Id == request.ProductId && x.IsActive, ct)) return BadRequest(new { error = "Choose an active product." });
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var source = await GetOrCreateStockAsync(request.FromBranchId, request.ProductId, ct);
        if (source.QuantityOnHand < request.Quantity) return BadRequest(new { error = "There is not enough stock to transfer." });
        source.QuantityOnHand -= request.Quantity;
        var destination = await GetOrCreateStockAsync(request.ToBranchId, request.ProductId, ct);
        if ((long)destination.QuantityOnHand + request.Quantity > int.MaxValue) return BadRequest(new { error = "Destination stock would exceed the supported limit." });
        destination.QuantityOnHand += request.Quantity;
        db.InventoryStockMovements.Add(new InventoryStockMovement
        {
            Type = "Branch transfer", BranchId = request.FromBranchId, DestinationBranchId = request.ToBranchId,
            ProductId = request.ProductId, Quantity = request.Quantity, QuantityChange = -request.Quantity,
            Reference = request.Reference?.Trim() ?? string.Empty, Notes = request.Notes?.Trim() ?? string.Empty,
            PerformedByUserId = UserId()
        });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return NoContent();
    }

    [HttpGet("stock/movements")]
    public async Task<ActionResult<IReadOnlyList<InventoryStockMovementDto>>> GetMovements([FromQuery] Guid branchId, CancellationToken ct)
    {
        if (!await IsAllowedBranchAsync(branchId, ct)) return Forbid();
        return Ok(await (from movement in db.InventoryStockMovements.AsNoTracking()
                         join product in db.Products.AsNoTracking() on movement.ProductId equals product.Id
                         join branch in db.Branches.AsNoTracking() on movement.BranchId equals branch.Id
                         where movement.BranchId == branchId || movement.DestinationBranchId == branchId
                         orderby movement.OccurredAtUtc descending
                         select new InventoryStockMovementDto(movement.Id, movement.Type, product.Sku, product.Name,
                             branch.Name, movement.DestinationBranchId == null ? null : db.Branches.Where(b => b.Id == movement.DestinationBranchId).Select(b => b.Name).FirstOrDefault(),
                             movement.Quantity, movement.DestinationBranchId == branchId ? movement.Quantity : movement.QuantityChange,
                             movement.Reference, movement.Notes, movement.OccurredAtUtc))
            .Take(200).ToListAsync(ct));
    }

    private async Task<string?> ValidatePurchaseOrderAsync(SavePurchaseOrderRequest request, CancellationToken ct)
    {
        if (!await IsAllowedBranchAsync(request.BranchId, ct)) return "The selected branch is not available to your account.";
        if (!await db.Suppliers.AnyAsync(x => x.Id == request.SupplierId && x.IsActive, ct)) return "Choose an active supplier.";
        if (request.Lines.Count == 0 || request.Lines.Count > 100) return "A purchase order must contain 1 to 100 lines.";
        if (request.Lines.Any(x => x.Quantity <= 0 || x.UnitCost < 0 || x.UnitCost > 9999999999999999.99m)) return "Line quantities must be positive and unit costs must fit within the supported price range.";
        if (request.Lines.GroupBy(x => x.ProductId).Any(g => g.Count() > 1)) return "A product can only appear once in an order.";
        if (request.Notes?.Trim().Length > 1000) return "Notes must be 1,000 characters or fewer.";
        if (request.ExpectedAtUtc is DateTime expected && expected < DateTime.UtcNow.Date.AddDays(-1)) return "Expected date cannot be in the past.";
        var ids = request.Lines.Select(x => x.ProductId).Distinct().ToArray();
        return await db.Products.CountAsync(x => ids.Contains(x.Id) && x.IsActive, ct) == ids.Length
            ? null : "Every order line must reference an active product.";
    }

    private async Task<PurchaseOrderDto?> ToDtoAsync(Guid id, CancellationToken ct) =>
        await db.PurchaseOrders.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new PurchaseOrderDto(x.Id, x.Number, x.BranchId, x.SupplierId,
                db.Suppliers.Where(s => s.Id == x.SupplierId).Select(s => s.Name).FirstOrDefault() ?? "(supplier removed)",
                x.Status, x.CreatedAtUtc, x.ExpectedAtUtc, x.OrderedAtUtc, x.Notes,
                x.Lines.Select(line => new PurchaseOrderLineDto(line.Id, line.ProductId,
                    db.Products.Where(p => p.Id == line.ProductId).Select(p => p.Sku).FirstOrDefault() ?? "",
                    db.Products.Where(p => p.Id == line.ProductId).Select(p => p.Name).FirstOrDefault() ?? "",
                    line.QuantityOrdered, line.QuantityReceived, line.UnitCost)).ToList()))
            .SingleOrDefaultAsync(ct);

    private async Task<BranchProductStock> GetOrCreateStockAsync(Guid branchId, Guid productId, CancellationToken ct)
    {
        var stock = await db.BranchProductStocks.SingleOrDefaultAsync(x => x.BranchId == branchId && x.ProductId == productId, ct);
        if (stock is not null) return stock;
        stock = new BranchProductStock { BranchId = branchId, ProductId = productId, QuantityOnHand = 0, ReorderLevel = 0 };
        db.BranchProductStocks.Add(stock);
        return stock;
    }

    private async Task IncreaseStockAsync(Guid branchId, Guid productId, int quantity, CancellationToken ct)
    {
        var stock = await GetOrCreateStockAsync(branchId, productId, ct);
        if ((long)stock.QuantityOnHand + quantity > int.MaxValue) throw new InvalidOperationException("Received stock would exceed the supported limit.");
        stock.QuantityOnHand += quantity;
    }

    private void AddMovement(Guid branchId, Guid productId, int delta, string type, string? reference, string notes, Guid? supplierId = null)
    {
        db.InventoryStockMovements.Add(new InventoryStockMovement
        {
            Type = type, BranchId = branchId, ProductId = productId, Quantity = Math.Abs(delta), QuantityChange = delta,
            SupplierId = supplierId, Reference = reference?.Trim() ?? string.Empty, Notes = notes.Trim(), PerformedByUserId = UserId()
        });
    }

    private Guid? UserId() => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : null;
    private bool IsBranchEnabled() => string.Equals(currentTenant.CompanySize, "Medium", StringComparison.OrdinalIgnoreCase);

    private async Task<bool> IsAllowedBranchAsync(Guid branchId, CancellationToken ct)
    {
        if (branchId == Guid.Empty || !await db.Branches.AnyAsync(b => b.Id == branchId && b.IsActive, ct)) return false;
        if (IsBranchEnabled()) return await branchAssignments.GetAssignedBranchIdAsync(User, ct) == branchId;
        await TenantBranchDefaults.EnsureMainBranchAsync(db, ct);
        var main = await db.Branches.AsNoTracking().Where(b => b.IsActive)
            .OrderBy(b => b.Code == "MAIN" ? 0 : 1).ThenBy(b => b.Name).Select(b => (Guid?)b.Id).FirstOrDefaultAsync(ct);
        return main == branchId;
    }
}

public sealed record PurchaseOrderLineRequest(Guid ProductId, int Quantity, decimal UnitCost);
public sealed record SavePurchaseOrderRequest(Guid BranchId, Guid SupplierId, DateTime? ExpectedAtUtc, string? Notes, List<PurchaseOrderLineRequest> Lines);
public sealed record ReceivePurchaseOrderLineRequest(Guid PurchaseOrderLineId, int Quantity);
public sealed record ReceivePurchaseOrderRequest(string? Reference, List<ReceivePurchaseOrderLineRequest> Lines);
public sealed record PurchaseOrderLineDto(Guid Id, Guid ProductId, string Sku, string ProductName, int QuantityOrdered, int QuantityReceived, decimal UnitCost);
public sealed record PurchaseOrderDto(Guid Id, string Number, Guid BranchId, Guid SupplierId, string SupplierName, string Status, DateTime CreatedAtUtc, DateTime? ExpectedAtUtc, DateTime? OrderedAtUtc, string Notes, IReadOnlyList<PurchaseOrderLineDto> Lines);
public sealed record LowStockDto(Guid ProductId, string Sku, string ProductName, int QuantityOnHand, int ReorderLevel);
public sealed record SetReorderLevelRequest(int ReorderLevel);
public sealed record AdjustInventoryStockRequest(Guid BranchId, Guid ProductId, int QuantityChange, string Reason, string? Reference);
public sealed record RecordStockCountRequest(Guid BranchId, Guid ProductId, int CountedQuantity, string? Reference, string? Notes);
public sealed record ReturnInventoryStockRequest(Guid BranchId, Guid ProductId, Guid SupplierId, int Quantity, string Reason, string? Reference);
public sealed record TransferInventoryStockRequest(Guid FromBranchId, Guid ToBranchId, Guid ProductId, int Quantity, string? Reference, string? Notes);
public sealed record InventoryStockMovementDto(Guid Id, string Type, string Sku, string ProductName, string BranchName, string? DestinationBranchName, int Quantity, int QuantityChange, string Reference, string Notes, DateTime OccurredAtUtc);
