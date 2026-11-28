using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Api.Modules.Sales;
using ThreadFlow.Api.Tenancy;
using ThreadFlow.Domain.Modules.Inventory;
using ThreadFlow.Domain.Modules.Sales;
using ThreadFlow.Infrastructure.data.Tenant;
using ThreadFlow.Infrastructure.data.Master;
using ThreadFlow.Infrastructure.Tenancy;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ThreadFlowRoles.SalesClerk)]
[Route("api/sales")]
public sealed class SalesController(
    CreateSale createSale,
    GetSalesCatalog getSalesCatalog,
    TenantDbContext db,
    MasterDbContext masterDb,
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
                    request.CashierShiftId,
                    UserId(),
                    request.CustomerId,
                    request.DiscountAmount,
                    request.DiscountReason ?? string.Empty,
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
                sale.Status,
                sale.DiscountAmount,
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

    [HttpGet("shift")]
    public async Task<ActionResult<CashierShiftResponse>> GetShift(
        [FromQuery] Guid branchId, CancellationToken cancellationToken)
    {
        if (!await IsAllowedBranchAsync(branchId, cancellationToken)) return Forbid();
        var userId = UserId();
        var shift = await db.CashierShifts.AsNoTracking()
            .Where(item => item.BranchId == branchId && item.OpenedByUserId == userId && item.Status == "Open")
            .OrderByDescending(item => item.OpenedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (shift is null) return NoContent();
        var cashSales = await GetCashSalesAsync(shift.Id, cancellationToken);
        return Ok(new CashierShiftResponse(shift.Id, shift.BranchId, shift.OpenedAtUtc,
            shift.OpeningCash, cashSales, shift.Status));
    }

    [HttpPost("shift/open")]
    public async Task<ActionResult<CashierShiftResponse>> OpenShift(
        [FromBody] OpenCashierShiftRequest request, CancellationToken cancellationToken)
    {
        if (!await IsAllowedBranchAsync(request.BranchId, cancellationToken)) return Forbid();
        if (request.OpeningCash < 0) return BadRequest(new { error = "Opening cash cannot be negative." });
        var userId = UserId();
        if (await db.CashierShifts.AnyAsync(item => item.BranchId == request.BranchId &&
                item.OpenedByUserId == userId && item.Status == "Open", cancellationToken))
            return Conflict(new { error = "You already have an open cashier shift at this branch." });

        var shift = new CashierShift
        {
            BranchId = request.BranchId,
            OpenedByUserId = userId,
            OpeningCash = decimal.Round(request.OpeningCash, 2)
        };
        db.CashierShifts.Add(shift);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new CashierShiftResponse(shift.Id, shift.BranchId, shift.OpenedAtUtc,
            shift.OpeningCash, 0m, shift.Status));
    }

    [HttpPost("shift/{id:guid}/close")]
    public async Task<ActionResult<CashierShiftResponse>> CloseShift(
        Guid id, [FromBody] CloseCashierShiftRequest request, CancellationToken cancellationToken)
    {
        if (request.CountedCash < 0) return BadRequest(new { error = "Counted cash cannot be negative." });
        var shift = await db.CashierShifts.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (shift is null) return NotFound(new { error = "Cashier shift not found." });
        if (!await IsAllowedBranchAsync(shift.BranchId, cancellationToken)) return Forbid();
        if (shift.OpenedByUserId != UserId()) return Forbid();
        if (shift.Status != "Open") return Conflict(new { error = "This cashier shift is already closed." });

        var cashPayments = await GetCashSalesAsync(id, cancellationToken);
        shift.ClosingCash = decimal.Round(request.CountedCash, 2);
        shift.ClosedByUserId = UserId();
        shift.ClosedAtUtc = DateTime.UtcNow;
        shift.Status = "Closed";
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new CashierShiftResponse(shift.Id, shift.BranchId, shift.OpenedAtUtc,
            shift.OpeningCash, cashPayments, shift.Status));
    }

    [HttpGet("customers")]
    public async Task<ActionResult<IReadOnlyList<SalesCustomerOption>>> GetCustomers(CancellationToken cancellationToken) =>
        Ok(await db.Customers.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new SalesCustomerOption(item.Id, item.Name, item.Phone, item.Email))
            .Take(1000).ToListAsync(cancellationToken));

    [HttpGet("holds")]
    public async Task<ActionResult<IReadOnlyList<HeldSaleSummary>>> GetHolds(
        [FromQuery] Guid branchId, CancellationToken cancellationToken)
    {
        if (!await IsAllowedBranchAsync(branchId, cancellationToken)) return Forbid();
        return Ok(await db.HeldSales.AsNoTracking().Where(item => item.BranchId == branchId && item.Status == "Held")
            .OrderBy(item => item.CreatedAtUtc)
            .Select(item => new HeldSaleSummary(item.Id, item.HoldNumber, item.CreatedAtUtc,
                item.Lines.Sum(line => line.Quantity), item.CustomerName))
            .ToListAsync(cancellationToken));
    }

    [HttpPost("holds")]
    public async Task<ActionResult<HeldSaleDto>> HoldSale(
        [FromBody] SaveHeldSaleRequest request, CancellationToken cancellationToken)
    {
        if (!await IsAllowedBranchAsync(request.BranchId, cancellationToken)) return Forbid();
        if (request.Lines is null || request.Lines.Count == 0 || request.Lines.Count > 100 ||
            request.Lines.Any(line => line.Quantity <= 0) || request.Lines.GroupBy(line => line.ProductId).Any(group => group.Count() > 1))
            return BadRequest(new { error = "A held sale needs 1 to 100 unique products with positive quantities." });
        if (request.CustomerName?.Length > 160)
            return BadRequest(new { error = "Customer name cannot exceed 160 characters." });
        if (request.CustomerId is Guid customerId && !await db.Customers.AnyAsync(item => item.Id == customerId, cancellationToken))
            return BadRequest(new { error = "The selected customer does not exist." });

        // Holding a cart takes no payment, so it does not need an open cashier shift;
        // the shift is still required to complete the sale later.
        var productIds = request.Lines.Select(line => line.ProductId).ToArray();
        if (await db.Products.CountAsync(item => productIds.Contains(item.Id) && item.IsActive, cancellationToken) != productIds.Length)
            return BadRequest(new { error = "Every held item must be an active product." });

        var hold = new HeldSale
        {
            HoldNumber = $"H-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6]}".ToUpperInvariant(),
            BranchId = request.BranchId,
            CashierUserId = UserId(),
            CustomerId = request.CustomerId,
            CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim(),
            Lines = request.Lines.Select(line => new HeldSaleLine { ProductId = line.ProductId, Quantity = line.Quantity }).ToList()
        };
        db.HeldSales.Add(hold);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new HeldSaleDto(hold.Id, hold.HoldNumber, hold.BranchId, hold.CustomerId, hold.CustomerName,
            hold.Lines.Select(line => new HeldSaleLineDto(line.ProductId, line.Quantity)).ToArray()));
    }

    [HttpPost("holds/{id:guid}/resume")]
    public async Task<ActionResult<HeldSaleDto>> ResumeHold(Guid id, CancellationToken cancellationToken)
    {
        var hold = await db.HeldSales.Include(item => item.Lines).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (hold is null) return NotFound(new { error = "Held sale not found." });
        if (!await IsAllowedBranchAsync(hold.BranchId, cancellationToken)) return Forbid();
        if (hold.Status != "Held") return Conflict(new { error = "This held sale is no longer available." });
        hold.Status = "Resumed";
        hold.ResumedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new HeldSaleDto(hold.Id, hold.HoldNumber, hold.BranchId, hold.CustomerId, hold.CustomerName,
            hold.Lines.Select(line => new HeldSaleLineDto(line.ProductId, line.Quantity)).ToArray()));
    }

    [HttpPost("holds/{id:guid}/cancel")]
    public async Task<IActionResult> CancelHold(Guid id, CancellationToken cancellationToken)
    {
        var hold = await db.HeldSales.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (hold is null) return NotFound(new { error = "Held sale not found." });
        if (!await IsAllowedBranchAsync(hold.BranchId, cancellationToken)) return Forbid();
        if (hold.Status != "Held") return Conflict(new { error = "This held sale is no longer available." });

        // The hold never touched stock, so cancelling only takes it out of the queue.
        hold.Status = "Cancelled";
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<SaleHistoryItem>>> GetHistory(
        [FromQuery] Guid branchId, CancellationToken cancellationToken)
    {
        if (!await IsAllowedBranchAsync(branchId, cancellationToken)) return Forbid();
        var sales = await db.Sales.AsNoTracking().Where(sale => sale.BranchId == branchId)
            .OrderByDescending(sale => sale.OccurredAtUtc).Take(250)
            .Select(sale => new
            {
                sale.Id,
                sale.SaleNumber,
                sale.OccurredAtUtc,
                sale.Status,
                sale.TotalAmount,
                Units = sale.Lines.Sum(line => line.Quantity),
                sale.CustomerId,
                sale.CashierUserId
            })
            .ToListAsync(cancellationToken);

        var saleIds = sales.Select(sale => sale.Id).ToArray();
        var cashierIds = sales.Where(sale => sale.CashierUserId.HasValue)
            .Select(sale => sale.CashierUserId!.Value).Distinct().ToArray();
        var cashierNames = cashierIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await masterDb.Users.AsNoTracking()
                .Where(user => user.TenantId == currentTenant.TenantId && cashierIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id,
                    user => user.Email ?? user.UserName ?? "Staff account", cancellationToken);
        var payments = await db.SalePayments.AsNoTracking()
            .Where(payment => saleIds.Contains(payment.SaleId))
            .Select(payment => new { payment.SaleId, payment.Method })
            .ToListAsync(cancellationToken);
        var paymentMethodsBySale = payments.GroupBy(payment => payment.SaleId)
            .ToDictionary(group => group.Key, group => string.Join(" + ",
                group.Select(payment => DisplayPaymentMethod(payment.Method)).Distinct()));

        return Ok(sales.Select(sale => new SaleHistoryItem(
            sale.Id,
            sale.SaleNumber,
            sale.OccurredAtUtc,
            sale.Status,
            sale.TotalAmount,
            sale.Units,
            sale.CustomerId,
            sale.CashierUserId is Guid cashierId && cashierNames.TryGetValue(cashierId, out var cashierName)
                ? cashierName
                : "Staff not recorded",
            paymentMethodsBySale.TryGetValue(sale.Id, out var paymentMethods)
                ? paymentMethods
                : "No payment recorded")));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDetailResponse>> GetSale(Guid id, CancellationToken cancellationToken)
    {
        var sale = await db.Sales.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (sale is null) return NotFound(new { error = "Sale not found." });
        if (!await IsAllowedBranchAsync(sale.BranchId, cancellationToken)) return Forbid();

        var lines = await (from line in db.SaleLines.AsNoTracking()
            join product in db.Products.AsNoTracking() on line.ProductId equals product.Id
            where line.SaleId == sale.Id
            orderby product.Sku
            select new SaleDetailLine(product.Sku, product.Name, product.Color, product.Size,
                line.Quantity, line.UnitPrice, line.LineTotal)).ToListAsync(cancellationToken);
        var payments = await db.SalePayments.AsNoTracking().Where(item => item.SaleId == sale.Id)
            .OrderBy(item => item.PaidAtUtc)
            .Select(item => new SalePaymentResponse(item.Method, item.Amount, item.PaidAtUtc))
            .ToListAsync(cancellationToken);

        return Ok(new SaleDetailResponse(sale.Id, sale.SaleNumber, sale.BranchId, sale.OccurredAtUtc,
            sale.Status, sale.DiscountAmount, sale.DiscountReason, sale.TotalAmount, lines, payments));
    }

    [HttpGet("availability/{productId:guid}")]
    public async Task<ActionResult<IReadOnlyList<BranchAvailability>>> GetAvailability(
        Guid productId, CancellationToken cancellationToken)
    {
        if (!await db.Products.AnyAsync(item => item.Id == productId && item.IsActive, cancellationToken))
            return NotFound(new { error = "Product not found." });
        // All branches belong to the current tenant database. Cashiers can check
        // availability elsewhere in their own company without changing their sale branch.
        var query = db.Branches.AsNoTracking().Where(branch => branch.IsActive);
        return Ok(await query.OrderBy(branch => branch.Name).Select(branch => new BranchAvailability(
            branch.Id, branch.Name,
            db.BranchProductStocks.Where(stock => stock.BranchId == branch.Id && stock.ProductId == productId)
                .Select(stock => (int?)stock.QuantityOnHand).FirstOrDefault() ?? 0))
            .ToListAsync(cancellationToken));
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

    private async Task<bool> IsAllowedBranchAsync(Guid branchId, CancellationToken cancellationToken)
    {
        if (!IsBranchEnabled())
            return await db.Branches.AnyAsync(branch => branch.Id == branchId && branch.IsActive, cancellationToken);
        return await branchAssignments.GetAssignedBranchIdAsync(User, cancellationToken) == branchId;
    }

    private Guid UserId() => Guid.TryParse(User.FindFirstValue("sub"), out var userId)
        ? userId
        : Guid.Empty;

    private static string DisplayPaymentMethod(PaymentMethod method) => method switch
    {
        PaymentMethod.EWallet => "E-Wallet",
        _ => method.ToString()
    };

    private async Task<decimal> GetCashSalesAsync(Guid shiftId, CancellationToken cancellationToken) =>
        await (from payment in db.SalePayments.AsNoTracking()
            join sale in db.Sales.AsNoTracking() on payment.SaleId equals sale.Id
            where sale.CashierShiftId == shiftId && sale.Status == "Completed" && payment.Method == PaymentMethod.Cash
            select (decimal?)payment.Amount).SumAsync(cancellationToken) ?? 0m;
}

public sealed record CreateSaleRequest(
    Guid BranchId,
    Guid CashierShiftId,
    Guid? CustomerId,
    decimal DiscountAmount,
    string? DiscountReason,
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
    string Status,
    decimal DiscountAmount,
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

public sealed record OpenCashierShiftRequest(Guid BranchId, decimal OpeningCash);
public sealed record CloseCashierShiftRequest(decimal CountedCash);
public sealed record CashierShiftResponse(Guid Id, Guid BranchId, DateTime OpenedAtUtc,
    decimal OpeningCash, decimal? CashSales, string Status);
public sealed record SalesCustomerOption(Guid Id, string Name, string? Phone, string? Email);
public sealed record SaveHeldSaleRequest(Guid BranchId, Guid? CustomerId, string? CustomerName, IReadOnlyList<CreateSaleLineRequest>? Lines);
public sealed record HeldSaleSummary(Guid Id, string HoldNumber, DateTime CreatedAtUtc, int Units, string? CustomerName);
public sealed record HeldSaleLineDto(Guid ProductId, int Quantity);
public sealed record HeldSaleDto(Guid Id, string HoldNumber, Guid BranchId, Guid? CustomerId,
    string? CustomerName, IReadOnlyList<HeldSaleLineDto> Lines);
public sealed record SaleHistoryItem(Guid Id, string SaleNumber, DateTime OccurredAtUtc,
    string Status, decimal TotalAmount, int Units, Guid? CustomerId,
    string CashierName, string PaymentMethods);
public sealed record SaleDetailResponse(Guid Id, string SaleNumber, Guid BranchId, DateTime OccurredAtUtc,
    string Status, decimal DiscountAmount, string DiscountReason, decimal TotalAmount,
    IReadOnlyList<SaleDetailLine> Lines, IReadOnlyList<SalePaymentResponse> Payments);
public sealed record SaleDetailLine(string Sku, string ProductName, string Color, string Size,
    int Quantity, decimal UnitPrice, decimal LineTotal);
public sealed record BranchAvailability(Guid BranchId, string BranchName, int QuantityOnHand);
