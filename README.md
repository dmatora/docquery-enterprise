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

## Backend configuration

The server uses an OpenAI-compatible chat completion provider.

Configuration policy:

- `apps/server/appsettings.json` keeps placeholder values in source control.
- Real provider values must come from user secrets or environment variables.
- Access to `/api/qa/*` requires a shared access key sent in the `X-Api-Access-Key` header.
- The API is expected to fail fast until valid settings are provided.

Set local secrets:

```powershell
dotnet user-secrets set "DocumentQa:Model" "<provider-model-name>" --project apps/server/Docquery.Server.csproj
dotnet user-secrets set "OpenAI:Endpoint" "https://<provider-host>/v1/" --project apps/server/Docquery.Server.csproj
dotnet user-secrets set "OpenAI:ApiKey" "<provider-api-key>" --project apps/server/Docquery.Server.csproj
dotnet user-secrets set "Security:AccessKey" "<shared-access-key>" --project apps/server/Docquery.Server.csproj
```

Generate a local shared access key in PowerShell:

```powershell
$secret = -join ([System.Security.Cryptography.MD5]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes((Get-Date).ToString())) | ForEach-Object { $_.ToString("x2") })
dotnet user-secrets set "Security:AccessKey" $secret --project apps/server/Docquery.Server.csproj
Write-Host "Generated Access Key: $secret"
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
$env:Security__AccessKey = "<shared-access-key>"
npm run serve:server
```

## Notes

- The npm scripts already force Nx to run without the daemon and without isolated plugin workers on this machine.
- Imported backend history from `docgen-enterprise` now lives in `apps/server`.
