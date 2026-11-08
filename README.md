# ThreadFlow

ThreadFlow is a multi-tenant ERP system with CRM capabilities. This repository is being organized as a layered .NET solution so the WinForms client, API, tenant persistence, and local/cloud synchronization can grow independently.

## Projects

- `ThreadFlow.Domain` - business concepts and rules
- `ThreadFlow.Application` - module use cases
- `ThreadFlow.Infrastructure` - database access, tenant routing, migrations, and sync persistence
- `ThreadFlow.Api` - HTTP API used by the desktop client and sync worker
- `ThreadFlow.Desktop` - WinForms user interface and shared theme
- `ThreadFlow.SyncWorker` - background local/cloud synchronization process

## Database plan

Each tenant will have one business database containing both ERP and CRM data. A separate master database will hold platform-level tenant and subscription information. Local and cloud copies will use matching migrations and tenant-scoped data synchronization. Database instances and credentials are not configured in this folder-setup step.

See [docs/architecture.md](docs/architecture.md) for the current structure and decisions.
