# Docquery

Nx monorepo for document analysis and question answering.

## Workspace

- `apps/client`: Angular frontend
- `apps/server`: ASP.NET Core Web API

## Prerequisites

- Node.js 22+
- npm
- .NET 8 SDK

## Install

```powershell
npm install
```

## Run

Frontend:

```powershell
npm run serve:client
```

Backend:

```powershell
npm run serve:server
```

Useful commands:

```powershell
npm run show:projects
npm run build:client
npm run build:server
npx nx test client
```

## Frontend runtime configuration

The Angular client now loads its API base URL at runtime from `apps/client/public/assets/runtime-config.json`. The production bundle is no longer tied to `environment.ts` file replacements for API routing.

Local development default:

```json
{
  "apiBaseUrl": "http://localhost:5152"
}
```

The Docker image uses `apps/client/public/assets/runtime-config.template.json` and expands it at container startup with `envsubst`.

## Frontend Docker image

Build the client image from the monorepo root:

```powershell
docker build -f apps/client/Dockerfile -t docquery-client .
```

Run the same image against any backend by changing only the container environment variable:

```powershell
docker run --rm -p 8080:80 -e DOCQUERY_API_BASE_URL="https://docquery.dmitry-matora.com" docquery-client
```

Nginx startup wiring lives in `apps/client/docker/40-runtime-config.sh`, and SPA routing is configured in `apps/client/nginx/default.conf`.

## Backend configuration

The server uses an OpenAI-compatible chat completion provider.

Configuration policy:

- `apps/server/appsettings.json` keeps placeholder values in source control.
- Real provider values must come from user secrets or environment variables.
- The API is expected to fail fast until valid settings are provided.

Set local secrets:

```powershell
dotnet user-secrets set "DocumentQa:Model" "<provider-model-name>" --project apps/server/Docquery.Server.csproj
dotnet user-secrets set "OpenAI:Endpoint" "https://<provider-host>/v1/" --project apps/server/Docquery.Server.csproj
dotnet user-secrets set "OpenAI:ApiKey" "<provider-api-key>" --project apps/server/Docquery.Server.csproj
```

Inspect or clear them:

```powershell
dotnet user-secrets list --project apps/server/Docquery.Server.csproj
dotnet user-secrets clear --project apps/server/Docquery.Server.csproj
```

Environment variable alternative:

```powershell
$env:DocumentQa__Model = "<provider-model-name>"
$env:OpenAI__Endpoint = "https://<provider-host>/v1/"
$env:OpenAI__ApiKey = "<provider-api-key>"
npm run serve:server
```

## Notes

- The npm scripts already force Nx to run without the daemon and without isolated plugin workers on this machine.
- Imported backend history from `docgen-enterprise` now lives in `apps/server`.
