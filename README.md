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

#### Document Models

| Container | Partition Key | Description |
|-----------|--------------|-------------|
| **Single** | `/title` | One movie document per movie with actors, directors, and reviews embedded |
| **Embedded** | `/title` | Movie documents + person documents with full movie data embedded |
| **Reference** | `/title` | Movie documents + person documents with minimal reference data |
| **Hybrid** | `/title` | Movie documents + one person document per unique person with a roles array |

### Indexing

Demonstrates how Cosmos DB indexing policies affect the RU cost of writes. A realistic browsing analytics event (with media, client, geo, performance, and context metadata) is written to three containers with different indexing strategies.

#### Containers

| Container | Partition Key | Indexing Strategy |
|-----------|--------------|-------------------|
| **Default** | `/sessionId` | All properties indexed (`/*`) — Cosmos DB default |
| **Implicit** | `/sessionId` | Excludes `/client/*`, `/geo/*`, `/performance/*`, `/context/*` from indexing |
| **Explicit** | `/sessionId` | Excludes `/*`, only includes `/timestamp/?` |

The `Indexing` database and containers are auto-created by the API on startup with the correct indexing policies. No processor or manual setup is needed.

#### Processor

The processor incrementally syncs movies from the [Battle Cabbage Media API](https://api.battlecabbage.com/docs) into all four containers. On each run it:

1. Queries the **Single** container for the highest `apiId` already stored
2. Fetches new movies from the API using `start_id`, `skip=1`, `order=asc` in batches of 50
3. Upserts documents into all four model containers
4. Repeats until the API returns fewer than 50 results

#### Indexing Policies

All containers exclude `/*` by default and only index the properties used in queries. Point reads (`ReadItemAsync` by `id` + partition key) do not use indexes.

**Single** — supports title lookups, person searches via `ARRAY_CONTAINS`, and the processor's `MAX(c.apiId)` aggregation:

```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    { "path": "/title/?" },
    { "path": "/type/?" },
    { "path": "/apiId/?" },
    { "path": "/actors/[]/name/?" },
    { "path": "/directors/[]/name/?" }
  ],
  "excludedPaths": [
    { "path": "/*" },
    { "path": "/\"_etag\"/?" }
  ]
}
```

**Embedded, Reference, Hybrid** — only support title lookups:

```json
{
  "indexingMode": "consistent",
  "automatic": true,
  "includedPaths": [
    { "path": "/title/?" }
  ],
  "excludedPaths": [
    { "path": "/*" },
    { "path": "/\"_etag\"/?" }
  ]
}
```

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
- `CosmosDB__IndexingDatabaseName` — Indexing database name (default: `Indexing`)

**Web** (`web/wwwroot/appsettings.json`):
- `ApiBaseUrl` — URL of the API (default: `http://localhost:5100`)

**Processor** (environment variables):
- `CosmosDB__Endpoint` — Cosmos DB endpoint URL (**required**)
- `CosmosDB__Key` — Cosmos DB key (omit to use DefaultAzureCredential / Managed Identity)
- `CosmosDB__DatabaseName` — Database name (default: `DataModeling`)
- `MediaApi__BaseUrl` — Battle Cabbage Media API base URL (default: `https://api.battlecabbage.com`)
- `Processor__BatchSize` — Movies per API call (1–100, default: `50`)
- `Processor__MaxBatches` — Max number of batches to process (omit for unlimited)

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

### 1. Create Azure Resources (in this order)

Create the following resources in Azure (all in the same resource group and region). **The order matters** — the API must be created before the Static Web App so the API URL is available:

1. **Cosmos DB Account** (NoSQL API) — Create a database named `DataModeling` with containers: `Single`, `Embedded`, `Reference`, `Hybrid` (partition key: `/title`)
2. **Container Apps Environment** — Shared environment for API and processor
3. **Container App (API)** — Image: `ghcr.io/<owner>/learncosmosdb/api:latest`, ingress enabled on port 8080. Note the API URL after creation.
4. **Container Apps Job (Processor)** — Image: `ghcr.io/<owner>/learncosmosdb/processor:latest`, trigger: manual
5. **Set GitHub variables** — Add `API_BASE_URL` (the API URL from step 3) to your GitHub repo variables **before** creating the Static Web App
6. **Static Web App** — When connecting to GitHub, Azure auto-generates a workflow file. You must add the API URL injection step to it (see below)

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

### 4. Static Web App Workflow

When you create the Static Web App and connect it to GitHub, Azure auto-generates a workflow file (e.g., `azure-static-web-apps-*.yml`). This workflow **does not** inject the production API URL by default. You must add the following step after `actions/checkout` and before `Azure/static-web-apps-deploy`:

```yaml
      - name: Set production API URL
        run: |
          cat > web/wwwroot/appsettings.json << EOF
          {
            "ApiBaseUrl": "${{ vars.API_BASE_URL }}"
          }
          EOF
```

This overwrites the localhost default with your Container App API URL at build time. **This step is required every time a new SWA workflow is auto-generated.**

### 5. Deploy

1. **Push to main:** GitHub Actions will build and deploy the API and web app automatically
2. **Seed the database:** Run the processor Container Apps Job from the Azure Portal to populate Cosmos DB
3. **Verify:** Visit your Static Web App URL
