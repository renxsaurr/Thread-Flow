using Microsoft.EntityFrameworkCore;
using ThreadFlow.Domain.Modules.TenantAdministration;
using ThreadFlow.Infrastructure.Tenancy;
using ThreadFlow.Infrastructure.data.Master;

namespace ThreadFlow.Api.Tenancy;

/// <summary>
/// Resolves a tenant before any tenant-scoped API endpoint can use TenantDbContext.
/// The header selector is intentionally available only during local development.
/// </summary>
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    private const string DevelopmentTenantHeader = "X-Tenant-Code";
    private const string TenantIdClaim = "tenant_id";

    public async Task InvokeAsync(
        HttpContext context,
        MasterDbContext masterDb,
        ICurrentTenantContext currentTenant,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/api/auth/login") ||
            context.Request.Path.StartsWithSegments("/api/auth/complete-invitation") ||
            context.Request.Path.StartsWithSegments("/api/onboarding") ||
            context.Request.Path.StartsWithSegments("/api/platform") ||
            context.Request.Path.StartsWithSegments("/api/health"))
        {
            await next(context);
            return;
        }

        Tenant? tenant;

        if (environment.IsDevelopment())
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                if (!Guid.TryParse(
                        context.User.FindFirst(TenantIdClaim)?.Value,
                        out var authenticatedTenantId))
                {
                    await WriteErrorAsync(
                        context,
                        StatusCodes.Status403Forbidden,
                        "This authenticated identity is not assigned to a tenant.");
                    return;
                }

                var requestedTenantCode = context.Request.Headers[DevelopmentTenantHeader]
                    .FirstOrDefault();
                var authenticatedTenantCode = context.User.FindFirst("tenant_code")?.Value;
                if (!string.IsNullOrWhiteSpace(requestedTenantCode) &&
                    !string.Equals(
                        requestedTenantCode,
                        authenticatedTenantCode,
                        StringComparison.OrdinalIgnoreCase))
                {
                    await WriteErrorAsync(
                        context,
                        StatusCodes.Status403Forbidden,
                        "The requested tenant does not match the authenticated user's tenant.");
                    return;
                }

                tenant = await masterDb.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        record => record.Id == authenticatedTenantId && record.IsActive,
                        context.RequestAborted);
            }
            else
            {
                var tenantCode = context.Request.Headers[DevelopmentTenantHeader]
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(tenantCode))
                    tenantCode = configuration["Tenant:DevelopmentTenantCode"];

                if (string.IsNullOrWhiteSpace(tenantCode))
                {
                    await WriteErrorAsync(
                        context,
                        StatusCodes.Status500InternalServerError,
                        "Set Tenant:DevelopmentTenantCode or send X-Tenant-Code for local development.");
                    return;
                }

                tenant = await masterDb.Tenants
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        record => record.Code == tenantCode && record.IsActive,
                        context.RequestAborted);
            }
        }
        else
        {
            if (context.User.Identity?.IsAuthenticated != true ||
                !Guid.TryParse(
                    context.User.FindFirst(TenantIdClaim)?.Value,
                    out var tenantId))
            {
                await WriteErrorAsync(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "An authenticated tenant identity is required.");
                return;
            }

            tenant = await masterDb.Tenants
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    record => record.Id == tenantId && record.IsActive,
                    context.RequestAborted);
        }

        if (tenant is null)
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status404NotFound,
                "The requested tenant was not found or is inactive.");
            return;
        }

        var databaseTarget = configuration["Tenant:DatabaseTarget"] ?? "Local";
        var databaseName = databaseTarget switch
        {
            "Local" => tenant.LocalDatabaseName,
            "Cloud" => tenant.CloudDatabaseName,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(databaseName))
        {
            await WriteErrorAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "The selected tenant database target is not configured.");
            return;
        }

        currentTenant.Set(tenant.Id, tenant.Code, tenant.CompanySize, databaseName);
        await next(context);
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int statusCode,
        string message)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
