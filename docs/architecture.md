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
- Each tenant gets one business database that contains both ERP and CRM records.
- Local and cloud environments each have a master database and a separate business database for every tenant.
- Tenant migrations are shared across all tenant business databases; master migrations are separate.
- Tenant routing is resolved by the application. Clients do not submit database connection strings.
- API startup reads the Master database from `ConnectionStrings:MasterErp`; use `ConnectionStrings__MasterErp` to override it outside local development.

## Local/cloud synchronization

Host the planned tenant-scoped synchronization service in the API project. It should use durable change queues, stable change identifiers, retry-safe delivery, checkpoints, and explicit conflict handling. Synchronize a tenant only with its own local/cloud database pair. Conflict ownership rules will be defined alongside the relevant business workflows.

## Desktop theme

The existing color token classes are kept in `ThreadFlow.WinForms/Theme/Colors`. `ThreadFlowTheme` builds on the existing `Pallets` class and applies the shared palette to forms and controls. New screens should call `ThreadFlowTheme.Apply(this)` after initialization.
