using Microsoft.EntityFrameworkCore;
using ThreadFlow.Infrastructure.data.Tenant;

namespace ThreadFlow.Api.Modules.ManagementApprovalsReporting;

public sealed record GetSalesOperationalReportQuery(
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtcExclusive);

public sealed record SalesOperationalReport(
    DateTime FromUtc,
    DateTime ToUtcExclusive,
    int SaleCount,
    int UnitsSold,
    decimal TotalSalesAmount);

public sealed class GetSalesOperationalReport(TenantDbContext db)
{
    public async Task<SalesOperationalReport> ExecuteAsync(
        GetSalesOperationalReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var fromUtc = query.FromUtc.UtcDateTime;
        var toUtcExclusive = query.ToUtcExclusive.UtcDateTime;

        if (fromUtc >= toUtcExclusive)
        {
            throw new ArgumentException(
                "The start date must be earlier than the end date.");
        }

        var salesInPeriod = db.Sales
            .Where(sale =>
                sale.OccurredAtUtc >= fromUtc &&
                sale.OccurredAtUtc < toUtcExclusive);

        var saleCount = await salesInPeriod.CountAsync(cancellationToken);

        var totalSalesAmount =
            await salesInPeriod
                .Select(sale => (decimal?)sale.TotalAmount)
                .SumAsync(cancellationToken)
            ?? 0m;

        var unitsSold =
            await (
                from line in db.SaleLines
                join sale in db.Sales on line.SaleId equals sale.Id
                where sale.OccurredAtUtc >= fromUtc &&
                      sale.OccurredAtUtc < toUtcExclusive
                select (int?)line.Quantity
            ).SumAsync(cancellationToken)
            ?? 0;

        return new SalesOperationalReport(
            fromUtc,
            toUtcExclusive,
            saleCount,
            unitsSold,
            totalSalesAmount);
    }
}