# Daniel Portfolio Web — CI/CD with GitHub Actions

Live portfolio deployed to Azure App Service via automated CI/CD pipeline.

🌐 **Live:** https://app-daniellab-2603.azurewebsites.net

---

## Architecture

```
git push → GitHub Actions → dotnet build → dotnet publish → Azure App Service
                                                                    ↓
                                                          VNet Integration
                                                                    ↓
                                                         VM (SQL Server 2025)
                                                                    ↓
                                                            DanielDB (BAK restored)
```

---

## Stack

| Component | Technology |
|---|---|
| Web App | ASP.NET Core 8 Minimal API |
| Database | SQL Server 2025 Express on Azure VM |
| Hosting | Azure App Service (Linux, .NET 8) |
| CI/CD | GitHub Actions |
| Networking | Azure VNet Integration |
| Secrets | App Service Connection Strings |

---

## CI/CD Pipeline

The workflow triggers automatically on every push to `main`:

```yaml
on:
  push:
    branches: [main]
```

**Steps:**
1. Checkout code
2. Setup .NET 8
3. `dotnet build --configuration Release`
4. `dotnet publish --configuration Release --output ./publish`
5. Deploy ZIP to Azure App Service via Publish Profile

**Average deploy time: ~3 minutes**

---

## Infrastructure Setup

### App Service
- **Name:** app-daniellab-2603
- **Plan:** asp-daniellab (Basic B1, Linux)
- **Runtime:** DOTNETCORE 8.0
- **VNet Integration:** snet-appservice (10.0.3.0/24)

### SQL Server
- **VM:** vm-sql01 (10.0.1.10)
- **Instance:** SQLEXPRESS
- **Version:** SQL Server 2025 Express (17.0.1000.7)
- **Database:** DanielDB
- **Port:** 1433 (TCP enabled, fixed port)

### Networking
- **VNet:** vnet-daniellab-2603 (10.0.0.0/16)
- **VM Subnet:** snet-default (10.0.1.0/24)
- **App Service Subnet:** snet-appservice (10.0.3.0/24)
- **Bastion:** AzureBastionSubnet (10.0.2.0/27)

---

## GitHub Secrets Required

| Secret | Description |
|---|---|
| `AZURE_WEBAPP_PUBLISH_PROFILE` | XML publish profile from Azure App Service |

---

## Database Schema

```sql
CREATE TABLE Proyectos (
    Id INT IDENTITY PRIMARY KEY,
    Titulo NVARCHAR(200),
    Tags NVARCHAR(500),
    Descripcion NVARCHAR(MAX)
);

CREATE TABLE Certificaciones (
    Id INT IDENTITY PRIMARY KEY,
    Nombre NVARCHAR(200),
    Entidad NVARCHAR(200),
    Skills NVARCHAR(500)
);
```

---

## Connection String

Configured as an App Service Connection String (type: SQLServer):

```
Server=10.0.1.10\SQLEXPRESS,1433;Database=DanielDB;User Id=sqladmin;Password=***;TrustServerCertificate=True;
```

Read in `Program.cs` via environment variable `SQLCONNSTR_DanielDB`.

---

## Screenshots

| Step | Screenshot |
|---|---|
| Git push portfolio | `cicd/01_git_push_portfolio.png` |
| Publish profile obtained | `cicd/02_publish_profile_output.png` |
| GitHub secret configured | `cicd/03_github_secret_created.png` |
| VNet Integration | `cicd/04_vnet_integration.png` |
| SQL Server 2025 installed | `cicd/05_sql_installed.png` |
| Workflow updated | `cicd/06_workflow_yml_updated.png` |
| Git push fix | `cicd/07_git_push_fix.png` |
| DanielDB restored | `cicd/08_danieldb_restored.png` |
| GitHub Actions success | `cicd/09_github_actions_success.png` |
| Connection string configured | `cicd/10_appservice_connection_string.png` |
| Web live with DB data | `cicd/11_webpage_projects.png` |

---

## Key Decisions

**Why env var instead of Key Vault?**
For lab/demo purposes the connection string is stored directly as an App Service Connection String. In production, Key Vault with Managed Identity would be the correct approach.

**Why SQL Server on VM instead of Azure SQL?**
Cost optimization for lab environment. Azure SQL Database would be the production-ready alternative.

**Why lazy SQL connection?**
The SQL connection is established inside the endpoint handler (not at startup) to prevent container timeout during cold starts on the App Service Linux plan.