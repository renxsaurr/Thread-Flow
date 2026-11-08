# ThreadFlow architecture and folder guide

## Solution projects

```text
src/
  ThreadFlow.Domain/          Business entities and rules, grouped by module
  ThreadFlow.Application/     Use cases and contracts, grouped by module
  ThreadFlow.Infrastructure/  Persistence, tenant routing, and sync storage
  ThreadFlow.Api/             HTTP endpoints, authentication, and middleware
  ThreadFlow.Desktop/         WinForms screens, API client, session, and theme
  ThreadFlow.SyncWorker/      Local/cloud sync worker, push, pull, and checkpoints
database/
  scripts/local/              Local SQL Server setup scripts
  scripts/cloud/              Cloud SQL Server setup/deployment scripts
  seeds/                      Repeatable demo seed data
docs/                         Architecture and project notes
```

## Module organization

The business modules are Tenant Administration, Inventory and Purchasing, Sales and Point of Sale, Customer Service and Loyalty (CRM), Manufacturing, Human Resources, Payroll, Finance and Accounting, Management/Approvals/Reporting, and Platform Administration. Domain and Application code is grouped by these modules; shared database and transport concerns stay in Infrastructure, API, and SyncWorker.

## Tenant database plan

- The master database holds platform-wide tenant registry and subscription data.
- Each tenant gets one business database that contains both ERP and CRM records.
- Local and cloud environments each have a master database and a separate business database for every tenant.
- Tenant migrations are shared across all tenant business databases; master migrations are separate.
- Tenant routing is resolved by the application. Clients do not submit database connection strings.

## Local/cloud synchronization

Use a tenant-scoped application sync process with durable change queues, stable change identifiers, retry-safe delivery, checkpoints, and explicit conflict handling. Synchronize a tenant only with its own local/cloud database pair. Conflict ownership rules will be defined alongside the relevant business workflows.

## Desktop theme

The existing color token classes are kept in `ThreadFlow.Desktop/Theme/Colors`. `ThreadFlowTheme` builds on the existing `Pallets` class and applies the shared palette to forms and controls. New screens should call `ThreadFlowTheme.Apply(this)` after initialization.
