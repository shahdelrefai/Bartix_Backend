# Bartrix

Backend scaffold for the Bartrix peer-to-peer marketplace.

## Solution Layout

- `src/Bartrix.Api`: ASP.NET Core host
- `src/Bartrix.SharedKernel`: shared domain primitives
- `src/Bartrix.BuildingBlocks`: shared technical infrastructure
- `src/Modules/*`: feature modules
- `tests/*`: solution test projects
- `docs/architecture`: architecture notes and decisions

## Current Status

This repository currently contains project structure only. Feature implementation starts in later tasks.

## Local Configuration

Do not store real secrets in tracked files.

Recommended local setup for the API project:

```bash
cd src/Bartrix.Api
dotnet user-secrets set "ConnectionStrings:Database" "<your-neon-connection-string>"
dotnet user-secrets set "Authentication:Jwt:SigningKey" "<a-long-random-secret>"
dotnet user-secrets set "Storage:Minio:AccessKey" "<your-minio-access-key>"
dotnet user-secrets set "Storage:Minio:SecretKey" "<your-minio-secret-key>"
```

The scaffold expects MinIO and PostgreSQL settings through configuration, and defaults to local MinIO development values only.
