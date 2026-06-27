# Railway Deployment Guide — Bartrix API

## Prerequisites

- GitHub repo with this code pushed
- Railway account at [railway.app](https://railway.app)
- Neon DB + AWS S3 already configured (see `docs/S3_SETUP.md`)

---

## Steps

### 1. Create a Railway Project

1. Go to [railway.app/new](https://railway.app/new)
2. **Deploy from GitHub repo** → select your repo
3. Railway detects the `Dockerfile` automatically

### 2. Set Environment Variables

In your Railway service → **Variables** tab, add every variable below.

| Variable | Value |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_HTTP_PORTS` | `${{PORT}}` |
| `ConnectionStrings__Database` | `Host=ep-snowy-mouse-allifdmt-pooler.c-3.eu-central-1.aws.neon.tech;Database=neondb;Username=neondb_owner;Password=<pw>;SSL Mode=Require;Trust Server Certificate=true` |
| `Authentication__Jwt__Issuer` | `Bartrix.Api` |
| `Authentication__Jwt__Audience` | `Bartrix.Mobile` |
| `Authentication__Jwt__SigningKey` | *(from `.env`)* |
| `Authentication__Jwt__AccessTokenMinutes` | `60` |
| `Authentication__Jwt__ClockSkewSeconds` | `60` |
| `Authentication__Google__AllowedAudiences__0` | `460987873980-s29ubpkkass8m9c106sf491oc6rbrhrl.apps.googleusercontent.com` |
| `Storage__S3__BucketName` | `bartix-684365645541-eu-north-1-an` |
| `Storage__S3__Region` | `eu-north-1` |
| `Storage__S3__AccessKey` | *(from `.env`)* |
| `Storage__S3__SecretKey` | *(from `.env`)* |
| `Storage__S3__ServiceUrl` | *(leave empty)* |
| `Storage__S3__ForcePathStyle` | `false` |
| `Storage__S3__CreateBucketIfMissing` | `false` |

> **Tip:** `${{PORT}}` is a Railway reference variable — Railway injects the actual port at runtime.
> Do NOT hardcode a number here.

### 3. Deploy

Railway auto-deploys on every push to your main branch. To trigger a manual deploy:
**Service → Deployments → Deploy Now**

Watch the build logs — a successful build ends with:
```
Now listening on: http://[::]:XXXX
Application started.
```

### 4. Verify

```bash
# Health check
curl https://<your-service>.up.railway.app/health
# → Healthy

# Register a user
curl -X POST https://<your-service>.up.railway.app/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test1234!","displayName":"Test"}'
# → { "accessToken": "...", "refreshToken": "..." }

# Upload an image (requires token from above)
curl -X POST https://<your-service>.up.railway.app/api/media \
  -H "Authorization: Bearer <token>" \
  -F "file=@photo.jpg"
# → { "objectName": "2026/06/...", "url": "https://bartix-...s3.amazonaws.com/..." }
```

---

## Notes

- **Database schema:** Created automatically on first startup by `IDatabaseInitializer`. No manual migration needed.
- **SignalR:** Works out of the box on a single Railway instance. If you scale to multiple instances later, add a Redis backplane.
- **OTP:** Currently uses `LocalMock` provider (phone OTP not live). To enable real SMS, add a provider implementation and set `Authentication__Otp__Provider` to the provider name.
- **Paymob / OpenAI:** Add their env vars when credentials are ready — the app starts fine without them (fields are left empty).

---

## Redeployment

Push to GitHub → Railway auto-deploys. The new container starts, runs database initializers (idempotent `CREATE TABLE IF NOT EXISTS`), then takes traffic.
