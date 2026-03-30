# daniel-portfolio-web

![Status](https://img.shields.io/badge/Status-Live-green)
![CI/CD](https://img.shields.io/badge/CI%2FCD-GitHub%20Actions-black?logo=github)
![Platform](https://img.shields.io/badge/Platform-Azure%20App%20Service-0078D4?logo=microsoft)
![Runtime](https://img.shields.io/badge/Runtime-.NET%208-512BD4?logo=dotnet)

ASP.NET Core 8 portfolio web application deployed to **Azure App Service** via automated CI/CD pipeline. Part of the [AD Migration Lab](https://github.com/DanielTeruel/ad-migration-azure) — this is the workload migrated from on-premises IIS to Azure App Service.

🌐 **Live:** https://app-daniellab-2603.azurewebsites.net

---

## What it does

Serves a dynamic portfolio page rendering projects and certifications from a SQL Server database. Content is fetched at runtime from **DanielDB** hosted on an Azure VM, reached via private VNet Integration — no public database endpoint.

---

## Architecture

```
git push → main
    │
    ▼
GitHub Actions
    ├── dotnet build
    ├── dotnet publish
    └── Deploy to Azure App Service
                │
                │  VNet Integration (snet-appservice · 10.0.3.0/24)
                ▼
        vm-sql01 — SQL Server 2025 Express
                │
                │  TCP 1433 (private IP only — never public)
                ▼
        DanielDB (Proyectos + Certificaciones)
```

---

## Stack

| Component | Technology |
|---|---|
| Web application | ASP.NET Core 8 |
| Database | SQL Server 2025 Express on Azure VM |
| Hosting | Azure App Service (Linux, B1) |
| CI/CD | GitHub Actions |
| Networking | Azure VNet Integration |
| Secrets | App Service Connection String |

---

## CI/CD Pipeline

Triggers automatically on every push to `main`. Average deploy time: ~3 minutes.

```yaml
on:
  push:
    branches: [main]
```

| Step | Action |
|---|---|
| Checkout | `actions/checkout@v4` |
| Setup .NET 8 | `actions/setup-dotnet@v4` |
| Build | `dotnet build --configuration Release` |
| Publish | `dotnet publish --configuration Release --output ./publish` |
| Deploy | `azure/webapps-deploy@v3` via Publish Profile |

### Secret required

| Secret | Description |
|---|---|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | XML publish profile from Azure App Service |

---

## Infrastructure

| Resource | Value |
|---|---|
| App Service | app-daniellab-2603 (Basic B1, Linux) |
| Runtime | DOTNETCORE 8.0 |
| VNet | vnet-daniellab-2603 (10.0.0.0/16) |
| App Service subnet | snet-appservice (10.0.3.0/24) |
| SQL VM | vm-sql01 (10.0.1.10) |
| SQL Instance | SQLEXPRESS · port 1433 (fixed) |
| SQL Version | SQL Server 2025 Express |
| Database | DanielDB |

Infrastructure provisioned via Bicep — see [04-iac/bicep](https://github.com/DanielTeruel/ad-migration-azure/tree/main/04-iac/bicep) in the migration lab repo.

---

## Database Schema

```sql
CREATE TABLE Proyectos (
    Id          INT IDENTITY PRIMARY KEY,
    Titulo      NVARCHAR(200),
    Tags        NVARCHAR(500),
    Descripcion NVARCHAR(MAX)
);

CREATE TABLE Certificaciones (
    Id      INT IDENTITY PRIMARY KEY,
    Nombre  NVARCHAR(200),
    Entidad NVARCHAR(200),
    Skills  NVARCHAR(500)
);
```

---

## Key Decisions

**Why App Service Connection String instead of Key Vault?**
For this lab, the connection string is stored as an App Service Connection String — exposed as `SQLCONNSTR_DanielDB` at runtime. The infrastructure already has Key Vault and Managed Identity configured in the Bicep template. In production, that would be the correct path: no credentials in App Service config, no manual rotation.

**Why lazy SQL connection?**
The SQL connection is opened inside the endpoint handler rather than at startup. This prevents container timeout during cold starts on the Linux B1 plan — the app starts and responds to the health check before attempting the database connection.

**Why a dedicated subnet for App Service?**
Azure VNet Integration requires a delegated subnet exclusively for the App Service — it cannot share the subnet used by the VM.

---

## Related

- [AD Migration Lab](https://github.com/DanielTeruel/ad-migration-azure) — full on-premises to Azure migration project this app is part of
- [CI/CD documentation](https://github.com/DanielTeruel/ad-migration-azure/tree/main/04-iac/cicd) — detailed setup steps, troubleshooting log, and pipeline run history
