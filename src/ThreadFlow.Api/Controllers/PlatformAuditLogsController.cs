using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThreadFlow.Api.Authentication;
using ThreadFlow.Infrastructure.data.Master;

namespace ThreadFlow.Api.Controllers;

[ApiController]
[Authorize(Roles = ThreadFlowRoles.PlatformAdmin)]
[Route("api/platform/audit-logs")]
public sealed class PlatformAuditLogsController(MasterDbContext masterDb) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PlatformAuditLogResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PlatformAuditLogResponse>>> Get(
        [FromQuery] int take = 100,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 500);
        var entries = await masterDb.PlatformAuditEntries
            .AsNoTracking()
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

        var entityUserIds = entries
            .Where(entry => entry.EntityType == "PlatformUser")
            .Select(entry => ParseGuid(entry.EntityId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value);
        var userIds = entries
            .Where(entry => entry.ActorUserId.HasValue)
            .Select(entry => entry.ActorUserId!.Value)
            .Concat(entityUserIds)
            .Distinct()
            .ToList();
        var users = userIds.Count == 0
            ? new Dictionary<Guid, AuditUser>()
            : await masterDb.Users
                .AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .Select(user => new AuditUser(user.Id, user.Email, user.TenantId))
                .ToDictionaryAsync(user => user.Id, cancellationToken);

        var applicationIds = entries
            .Where(entry => entry.EntityType == "CompanyApplication")
            .Select(entry => ParseGuid(entry.EntityId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var applications = applicationIds.Count == 0
            ? new Dictionary<Guid, AuditApplication>()
            : await masterDb.CompanyApplications
                .AsNoTracking()
                .Where(application => applicationIds.Contains(application.Id))
                .Select(application => new AuditApplication(
                    application.Id,
                    application.CompanyName,
                    application.TenantId))
                .ToDictionaryAsync(application => application.Id, cancellationToken);

        var tenantIds = entries
            .Where(entry => entry.TenantId.HasValue)
            .Select(entry => entry.TenantId!.Value)
            .Concat(entries
                .Where(entry => entry.EntityType == "TenantCompanySettings")
                .Select(entry => ParseGuid(entry.EntityId))
                .Where(id => id.HasValue)
                .Select(id => id!.Value))
            .Concat(users.Values
                .Where(user => user.TenantId.HasValue)
                .Select(user => user.TenantId!.Value))
            .Concat(applications.Values
                .Where(application => application.TenantId.HasValue)
                .Select(application => application.TenantId!.Value))
            .Distinct()
            .ToList();
        var tenants = tenantIds.Count == 0
            ? new Dictionary<Guid, AuditTenant>()
            : await masterDb.Tenants
                .AsNoTracking()
                .Where(tenant => tenantIds.Contains(tenant.Id))
                .Select(tenant => new AuditTenant(tenant.Id, tenant.Name, tenant.Code))
                .ToDictionaryAsync(tenant => tenant.Id, cancellationToken);

        var response = entries.Select(entry =>
        {
            users.TryGetValue(entry.ActorUserId ?? Guid.Empty, out var actor);
            var entityId = ParseGuid(entry.EntityId);
            Guid? tenantId = entry.TenantId;
            AuditApplication? application = null;

            if (tenantId is null && entry.EntityType == "PlatformUser" &&
                entityId is Guid userId && users.TryGetValue(userId, out var entityUser))
            {
                tenantId = entityUser.TenantId;
            }
            else if (entry.EntityType == "TenantCompanySettings" && tenantId is null)
            {
                tenantId = entityId;
            }
            else if (entry.EntityType == "CompanyApplication" && entityId is Guid applicationId &&
                     applications.TryGetValue(applicationId, out application))
            {
                tenantId ??= application.TenantId;
            }

            var tenantLabel = tenantId is Guid resolvedTenantId &&
                              tenants.TryGetValue(resolvedTenantId, out var tenant)
                ? tenant.Name
                : application is not null
                    ? application.CompanyName
                    : "Platform";

            return new PlatformAuditLogResponse(
                entry.Id,
                entry.ActorUserId,
                actor?.Email,
                tenantLabel,
                entry.Action,
                entry.EntityType,
                entry.EntityId,
                entry.Summary,
                entry.OccurredAtUtc);
        }).ToList();

        return Ok(response);
    }

    private static Guid? ParseGuid(string value) =>
        Guid.TryParse(value, out var id) ? id : null;

    private sealed record AuditUser(Guid Id, string? Email, Guid? TenantId);
    private sealed record AuditApplication(Guid Id, string CompanyName, Guid? TenantId);
    private sealed record AuditTenant(Guid Id, string Name, string Code);
}

public sealed record PlatformAuditLogResponse(
    Guid Id,
    Guid? ActorUserId,
    string? ActorEmail,
    string Tenant,
    string Action,
    string EntityType,
    string EntityId,
    string Summary,
    DateTime OccurredAtUtc);
