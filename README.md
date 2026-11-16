# ThreadFlow

ThreadFlow is a multi-tenant ERP system with CRM capabilities, organized into the professor's four-project structure: Domain, Infrastructure, API, and Desktop.

## Projects

- `ThreadFlow.Domain` - business entities and rules
- `ThreadFlow.Infrastructure` - database contexts in `data`, plus separate Master and Tenant migrations
- `ThreadFlow.Api` - HTTP API, module workflows, and hosted background synchronization
- `ThreadFlow.WinForms` - Windows Forms user interface and shared theme

## Database plan

Each tenant will have one business database containing both ERP and CRM data. A separate master database will hold platform-level tenant and subscription information. Local and cloud copies will use matching migrations and tenant-scoped data synchronization. The API's `MasterErp` connection setting defaults to local SQL Server; override it with the `ConnectionStrings__MasterErp` environment variable for another environment. Do not put cloud credentials in source-controlled settings.

See [docs/architecture.md](docs/architecture.md) for the current structure and decisions.
