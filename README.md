# Learn Cosmos DB

An interactive learning platform for Azure Cosmos DB concepts. Each module demonstrates a different Cosmos DB topic through a web application backed by a REST API.

## Architecture

| Component | Technology | Deployment Target |
|-----------|-----------|-------------------|
| **web/** | Blazor WebAssembly (.NET 9) | Azure Static Web Apps |
| **api/** | ASP.NET Core minimal API (.NET 9) | Azure Container Apps |
| **processor/** | .NET 9 Console App | Azure Container Apps Job |
| **shared/** | .NET 9 Class Library | Referenced by all projects |

## Modules

### Data Modeling
Demonstrates how document modeling in Cosmos DB affects query cost (RU), performance, and user experience using a movie database. Compares four models: Single, Embedded, Reference, and Hybrid.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://docs.docker.com/get-docker/) & Docker Compose
- An Azure Cosmos DB account (or use the local emulator via Docker)

## Local Development

### Quick Start with Docker Compose

```bash
docker compose up --build
```

This starts:
- **Cosmos DB Emulator** on `https://localhost:8081`
- **API** on `http://localhost:5100`
- **Web App** on `http://localhost:5200`

### Running Individually

```bash
# API
cd api
dotnet run

# Web (in a separate terminal)
cd web
dotnet run
```

### Configuration

**API** (`api/appsettings.json` or environment variables):
- `CosmosDB__Endpoint` — Cosmos DB endpoint URL
- `CosmosDB__Key` — Cosmos DB key (omit to use DefaultAzureCredential)
- `CosmosDB__DatabaseName` — Database name (default: `DataModeling`)

**Web** (`web/wwwroot/appsettings.json`):
- `ApiBaseUrl` — URL of the API (default: `http://localhost:5100`)

**Processor** (environment variables):
- `MediaApi__BaseUrl` — Battle Cabbage Media API base URL (default: `https://api.battlecabbage.com`)
- `MediaApi__ApiKey` — API key for write operations (optional — only needed for `/generate`)
- `CosmosDB__Endpoint` — Cosmos DB endpoint URL
- `CosmosDB__DatabaseName` — Database name (default: `DataModeling`)

## Project Structure

```
LearnCosmosDB/
├── web/           → Blazor WebAssembly frontend
├── api/           → ASP.NET Core minimal API
├── processor/     → API-to-Cosmos document builder
├── shared/        → Shared models library
├── .github/workflows/
│   ├── ci.yml              → Build & test on PR/push
│   ├── deploy-api.yml      → Build, push & deploy API container
│   ├── deploy-web.yml      → Build & deploy Blazor WASM to Static Web Apps
│   └── deploy-processor.yml → Build & push processor container
├── docker-compose.yml
└── LearnCosmosDB.sln
```

## Azure Deployment

### Prerequisites

- An Azure subscription
- A GitHub repository with this code pushed

### 1. Create Azure Resources

Create the following resources in Azure (all in the same resource group and region):

| Resource | Type | Notes |
|----------|------|-------|
| Cosmos DB Account | NoSQL API | Create a database named `DataModeling` with containers: `Single`, `Embedded`, `Reference`, `Hybrid` (partition key: `/title`) |
| Container Apps Environment | — | Shared environment for API and processor |
| Container App (API) | Container App | Image: `ghcr.io/<owner>/<repo>/api:latest`, ingress enabled on port 8080 |
| Container Apps Job (Processor) | Container Apps Job | Image: `ghcr.io/<owner>/<repo>/processor:latest`, trigger: manual |
| Static Web App | Free/Standard | For the Blazor WASM frontend |

### 2. Configure Container App Environment Variables

**API Container App:**
| Variable | Value |
|----------|-------|
| `CosmosDB__Endpoint` | Your Cosmos DB account endpoint URL |
| `CosmosDB__Key` | Your Cosmos DB account key (or omit and use Managed Identity) |
| `CosmosDB__DatabaseName` | `DataModeling` |
| `AllowedOrigins__0` | Your Static Web App URL (e.g., `https://<name>.azurestaticapps.net`) |

**Processor Container Apps Job:**
| Variable | Value |
|----------|-------|
| `CosmosDB__Endpoint` | Your Cosmos DB account endpoint URL |
| `CosmosDB__Key` | Your Cosmos DB account key (or omit and use Managed Identity) |
| `CosmosDB__DatabaseName` | `DataModeling` |
| `MediaApi__BaseUrl` | `https://api.battlecabbage.com` |

### 3. Configure GitHub Repository

**Repository variables** (Settings → Secrets and variables → Actions → Variables):
| Variable | Value |
|----------|-------|
| `AZURE_RESOURCE_GROUP` | Your Azure resource group name |
| `API_CONTAINER_APP_NAME` | Your API Container App name |
| `API_BASE_URL` | Your API's public URL (e.g., `https://<api-name>.<region>.azurecontainerapps.io`) |

**Repository secrets** (Settings → Secrets and variables → Actions → Secrets):
| Secret | Value |
|--------|-------|
| `AZURE_CLIENT_ID` | App registration client ID (for OIDC federated login) |
| `AZURE_TENANT_ID` | Azure AD tenant ID |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription ID |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | Deployment token from your Static Web App (found in Azure Portal → Static Web App → Manage deployment token) |

> **Note:** The API and processor workflows authenticate to GHCR using the built-in `GITHUB_TOKEN` — no additional container registry secrets are needed. The API deploy workflow uses `azure/container-apps-deploy-action` which requires an Azure login — configure `AZURE_CREDENTIALS` or use federated identity (OIDC).

### 4. Deploy

1. **Seed the database:** Run the processor Container Apps Job from the Azure Portal to populate Cosmos DB
2. **Push to main:** GitHub Actions will build and deploy the API and web app automatically
3. **Verify:** Visit your Static Web App URL
