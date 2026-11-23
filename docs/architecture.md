# ThreadFlow architecture and folder guide

## Solution projects

```text
src/
  ThreadFlow.Domain/          Entities and business rules
    Entities/                 ThreadFlow entity classes
  ThreadFlow.Infrastructure/  Database access, tenant routing, and migrations
    data/                     Master and Tenant EF Core DbContexts and design-time factories
    Migrations/               Separate Master and Tenant migration histories
  ThreadFlow.Api/             HTTP endpoints, module workflows, authentication, middleware, and hosted sync
    Controllers/              HTTP request and response endpoints
    Modules/                  Module-specific workflow code, grouped by business module
    BackgroundServices/       Hosted API services, including planned synchronization
  ThreadFlow.WinForms/        Windows Forms screens, API client, session, and theme
database/
  scripts/local/              Local SQL Server setup scripts
  scripts/cloud/              Cloud SQL Server setup/deployment scripts
  seeds/                      Repeatable demo seed data
docs/                         Architecture and project notes
```

## Module organization

The business modules are Tenant Administration, Inventory and Purchasing, Sales and Point of Sale, Customer Service and Loyalty (CRM), Manufacturing, Human Resources, Payroll, Finance and Accounting, Management/Approvals/Reporting, and Platform Administration. Domain concepts are grouped by module in their namespaces. API controllers handle HTTP requests and module workflows implement the operations; Infrastructure handles database persistence and migrations. The WinForms client communicates with the API over HTTP and does not reference the API project directly.

The PDF's example names `Company` and `CompanyDatabase` are represented by ThreadFlow's existing `Tenant` registry entity and its local/cloud database-name fields. Keeping that model avoids a database schema change during this folder-structure alignment. The existing Master and Tenant contexts remain separate.

## Tenant database plan

- The master database holds platform-wide tenant registry and subscription data.
- Tenant A, Tenant B, and Tenant C are logical hosting groups for Micro, Small, and Medium companies. The master registry records each company's group and its own business database name.
- Each company gets one separate business database that contains both ERP and CRM records. Grouping is metadata and a database-name prefix; SQL Server databases remain peers on the configured server and are not nested inside group databases.
- Local and cloud environments each have a master database and a separate business database for every tenant.
- Tenant migrations are shared across all tenant business databases; master migrations are separate.
- Tenant routing is resolved by the API. Clients never submit database connection strings.
- In local Development, `X-Tenant-Code` may select an active seeded tenant; if omitted, `Tenant:DevelopmentTenantCode` is used. This selector is disabled outside Development.
- `POST /api/auth/login` verifies credentials against ASP.NET Identity in Master. Company users are routed from the tenant assignment on their account; an optional tenant code, if supplied, must match that assignment. Platform administrators receive no tenant claim.
- A prospective company submits `POST /api/onboarding/applications` with contact details and a requested Micro, Small, or Medium plan. The application is stored in Master as Pending and recorded in the platform audit log.
- Platform Admins review applications through `GET /api/platform/applications`, then approve or reject an application. Approval provisions a separate tenant database and subscription, activates the tenant, assigns the applicant as its Tenant Admin, and returns a one-time setup token. The token is shown to the operator for manual handoff because email delivery is not configured. A provisioning failure leaves the application in Provisioning so approval can be retried.
- The first Tenant Admin completes the invitation through `POST /api/auth/complete-invitation`, setting their password and initial company profile, tax, and receipt settings. After sign-in, Tenant Admins can update those settings through `GET/PUT /api/tenant/company-settings` and invite employees through the tenant user endpoints.
- Platform Admins can review platform events through `GET /api/platform/audit-logs`. Application submission/review, tenant employee invitations, invitation completion, and company settings changes are recorded.
- The legacy `POST /api/platform/tenants/{tenantCode}/tenant-admin` endpoint remains available for existing lab/demo tenants; new company onboarding uses the application approval workflow above.
- Tenant Admins manage their company's staff through `GET /api/tenant/users/roles`, `GET /api/tenant/users`, and `POST /api/tenant/users`. The tenant ID comes from the authenticated tenant context, never from the request body. The initial assignable roles are `SalesClerk` and `CustomerServiceAgent`; tenant admins cannot assign `TenantAdmin` or `PlatformAdmin` through this endpoint.
- Tenant business APIs enforce role authorization: `SalesClerk` and `TenantAdmin` can use sales; `CustomerServiceAgent` and `TenantAdmin` can use customer interactions; operational reports and tenant user management are limited to `TenantAdmin`. The WinForms client signs in through the API, sends its bearer token with requests, and displays modules allowed by the signed-in role.
- Staff creation returns a one-time password-setup token instead of asking the Tenant Admin to choose the employee's password. `POST /api/auth/complete-invitation` lets that employee set their own password. Email delivery is not configured yet, so local development returns the token to the Tenant Admin for manual handoff.
- Login returns a 30-minute signed bearer token. Tenant tokens carry the trusted `tenant_id` and `tenant_code`; platform administrator tokens carry the `PlatformAdmin` role and no tenant assignment. Platform administrators therefore do not receive access to tenant business data through this identity.
- Outside Development, tenant routing requires an authenticated identity with a trusted `tenant_id` claim. `UseAuthentication` runs before `TenantResolutionMiddleware`; tenant-scoped API requests fail closed with 401 when the claim is absent.
- Configure `Authentication:Jwt:SigningKey` outside Development, using at least 32 UTF-8 bytes and keeping the secret outside source control. Development generates an ephemeral key when none is configured, so tokens expire across API restarts.
- The first platform administrator can be bootstrapped by setting both `Bootstrap:PlatformAdminEmail` and `Bootstrap:PlatformAdminPassword` in environment variables or user secrets. Bootstrap creates an account only when no platform administrator exists and does not reset an existing account.
- The API looks up the active company tenant in Master, which also records its logical hosting group, selects its configured local/cloud database name, and applies that name to the server connection string before constructing `TenantDbContext`.
- API startup reads the Master database from `ConnectionStrings:MasterErp`; use `ConnectionStrings__MasterErp` to override it outside local development.

## Local/cloud synchronization

Host the planned tenant-scoped synchronization service in the API project. It should use durable change queues, stable change identifiers, retry-safe delivery, checkpoints, and explicit conflict handling. Synchronize a tenant only with its own local/cloud database pair. Conflict ownership rules will be defined alongside the relevant business workflows.

## Desktop theme

The existing color token classes are kept in `ThreadFlow.WinForms/Theme/Colors`. `ThreadFlowTheme` builds on the existing `Pallets` class and applies the shared palette to forms and controls. New screens should call `ThreadFlowTheme.Apply(this)` after initialization.
