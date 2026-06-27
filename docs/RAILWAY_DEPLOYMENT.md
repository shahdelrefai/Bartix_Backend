# Railway Deployment — Bartrix API

## Overview

The Bartrix API is deployed on Railway at:

**`https://humble-education-production-60e4.up.railway.app`**

- Platform: Railway (free tier)
- Region: EU (Frankfurt)
- Runtime: Docker container (.NET 8 ASP.NET Core)
- Database: Neon PostgreSQL (serverless, eu-central-1)
- Storage: AWS S3 (eu-north-1)

---

## What Was Done

### 1. Dockerfile

Created a multi-stage Dockerfile at the repo root:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish "src/Bartrix.Api/Bartrix.Api.csproj" \
    -c Release -o /app/publish --nologo

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Bartrix.Api.dll"]
```

**Note:** The `mcr.microsoft.com/dotnet/aspnet:8.0` base image already includes an `app` user — adding `RUN adduser` causes a build failure. Removed it.

### 2. `.dockerignore`

Excludes secrets and unnecessary files from the Docker build context:

```
.env
bin/
obj/
**/.vs/
**/.vscode/
tests/
docs/
deploy/
*.user
*.md
nohup.out
.git/
```

### 3. `railway.json`

Tells Railway to use the Dockerfile and configures health checks:

```json
{
  "$schema": "https://railway.app/railway.schema.json",
  "build": {
    "builder": "DOCKERFILE",
    "dockerfilePath": "Dockerfile"
  },
  "deploy": {
    "healthcheckPath": "/health",
    "healthcheckTimeout": 300,
    "restartPolicyType": "ON_FAILURE",
    "restartPolicyMaxRetries": 10
  }
}
```

### 4. `appsettings.Production.json`

Production-specific config overrides (committed to repo — no secrets here):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.Hosting.Lifetime": "Information",
      "Microsoft.AspNetCore.DataProtection": "Error"
    }
  },
  "Authentication": {
    "Otp": {
      "DevelopmentCode": null
    }
  },
  "Realtime": {
    "SignalR": {
      "EnableDetailedErrors": false
    }
  }
}
```

**Why `DevelopmentCode: null`:** Clears the `123456` bypass code from `appsettings.json` so it can't be used in production.

**Why suppress DataProtection logs:** The app uses JWT, not cookies — DataProtection keys are unused. The warning is noise.

---

## Deployment Steps

### Step 1 — Install Railway CLI

```bash
brew install railway
```

### Step 2 — Login

```bash
railway login   # opens browser OAuth
```

### Step 3 — Initialize Project

```bash
cd "Bartrix copy/"
railway init    # creates project "humble-education" on Railway
railway link --project humble-education
```

### Step 4 — Initial Deploy

```bash
railway up --detach --service humble-education
```

This uploads the local code, builds the Docker image on Railway, and starts the container.

### Step 5 — Set Environment Variables

```bash
railway variables --set "KEY=VALUE"
```

All variables set (see table below).

### Step 6 — Rename Service

Railway CLI doesn't have a rename command. Done via GraphQL API:

```bash
curl -X POST https://backboard.railway.com/graphql/v2 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"query":"mutation { serviceUpdate(id: \"SERVICE_ID\", input: { name: \"bartrix_backend_api\" }) { id name } }"}'
```

### Step 7 — Generate Public Domain

```bash
railway domain   # → https://humble-education-production-60e4.up.railway.app
```

### Step 8 — Auto-Deploy via GitHub Actions

Railway's native GitHub App integration had access issues. Solved with a GitHub Actions workflow instead:

1. Created a Railway project token via GraphQL API
2. Added it as GitHub secret `RAILWAY_TOKEN`
3. Created `.github/workflows/deploy.yml`

```yaml
name: Deploy to Railway
on:
  push:
    branches: [main]
jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Install Railway CLI
        run: npm install -g @railway/cli
      - name: Deploy
        run: railway up --service bartrix_backend_api --detach
        env:
          RAILWAY_TOKEN: ${{ secrets.RAILWAY_TOKEN }}
```

Now every push to `main` → GitHub Actions → Railway redeploys automatically.

---

## Environment Variables

Set in Railway service → Variables tab:

| Variable | Value |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_HTTP_PORTS` | `8080` |
| `ConnectionStrings__Database` | Neon connection string |
| `Authentication__Jwt__Issuer` | `Bartrix.Api` |
| `Authentication__Jwt__Audience` | `Bartrix.Mobile` |
| `Authentication__Jwt__SigningKey` | 256-bit hex key |
| `Authentication__Jwt__AccessTokenMinutes` | `60` |
| `Authentication__Jwt__ClockSkewSeconds` | `60` |
| `Authentication__Google__AllowedAudiences__0` | Google OAuth client ID |
| `Storage__S3__BucketName` | `bartix-684365645541-eu-north-1-an` |
| `Storage__S3__Region` | `eu-north-1` |
| `Storage__S3__AccessKey` | IAM access key |
| `Storage__S3__SecretKey` | IAM secret key |
| `Storage__S3__ServiceUrl` | *(empty — uses real S3)* |
| `Storage__S3__ForcePathStyle` | `false` |
| `Storage__S3__CreateBucketIfMissing` | `false` |
| `Paymob__BaseUrl` | `https://accept.paymob.com/api` |

---

## Bugs Fixed During Deployment

### 1. `adduser` failure in Dockerfile

**Error:** `adduser: The user 'app' already exists`

**Cause:** `mcr.microsoft.com/dotnet/aspnet:8.0` already ships with an `app` user.

**Fix:** Removed the `RUN adduser` line from the Dockerfile.

### 2. OTP crash in production

**Error:** `System.ArgumentException: The value cannot be an empty string (Parameter 'password')`

**Cause:** `InMemoryEmailOtpService.GenerateCode()` returned `_options.DevelopmentCode` when provider is `LocalMock` — but `DevelopmentCode` is `null` in production (set via `appsettings.Production.json`). Passing `null` to the password hasher threw an exception.

**Fix:** Only use `DevelopmentCode` when it's non-null/non-empty; otherwise generate a real random code:

```csharp
private string GenerateCode()
{
    if (string.Equals(_options.Provider, "LocalMock", StringComparison.OrdinalIgnoreCase)
        && !string.IsNullOrWhiteSpace(_options.DevelopmentCode))
        return _options.DevelopmentCode;

    var maxValue = (int)Math.Pow(10, _options.CodeLength);
    return RandomNumberGenerator.GetInt32(0, maxValue).ToString($"D{_options.CodeLength}");
}
```

In production the OTP is now a real random 6-digit code logged to Railway logs.

---

## Verification

```bash
# Health check
curl https://humble-education-production-60e4.up.railway.app/health
# → Healthy

# Service info
curl https://humble-education-production-60e4.up.railway.app/
# → {"service":"Bartrix.Api","status":"Running"}

# Login
curl -X POST https://humble-education-production-60e4.up.railway.app/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"...","password":"..."}'
# → {"accessToken":"...","refreshToken":"..."}
```

---

## Railway Project Details

| Property | Value |
|----------|-------|
| Project name | `humble-education` |
| Project ID | `b2550981-9b3c-4267-a109-9dc09915855f` |
| Service name | `bartrix_backend_api` |
| Service ID | `32e09c6f-5b61-42db-93e6-91873fb186a5` |
| Environment | `production` |
| Environment ID | `e2d96bb7-bbbd-44fe-b4f7-7194901ea1d6` |
| Public URL | `https://humble-education-production-60e4.up.railway.app` |
| GitHub repo | `github.com/shahdelrefai/Bartix_Backend` |
| Auto-deploy branch | `main` (via GitHub Actions) |
