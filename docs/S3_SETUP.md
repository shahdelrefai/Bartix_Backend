# AWS S3 Setup — Bartrix Media Storage

Replaces local MinIO with AWS S3 for production image storage.
Local development still uses MinIO at `localhost:9000` — no changes needed there.

---

## What Was Done

### 1. AWS Console

#### Create S3 Bucket
1. Go to [s3.console.aws.amazon.com](https://s3.console.aws.amazon.com) → **Create bucket**
2. Bucket name: `bartix-684365645541-eu-north-1-an`
3. Region: `eu-north-1` (Stockholm)
4. Uncheck **Block all public access** → confirm

#### Add Public Read Policy
1. Open the bucket → **Permissions** tab
2. Scroll to **Bucket policy** → **Edit** → paste:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "PublicRead",
      "Effect": "Allow",
      "Principal": "*",
      "Action": "s3:GetObject",
      "Resource": "arn:aws:s3:::bartix-684365645541-eu-north-1-an/*"
    }
  ]
}
```

> **Important:** The `Resource` must end with `/*` — without it you get  
> `Action does not apply to any resource(s) in statement`

3. Save changes

#### Create IAM User
1. Go to [console.aws.amazon.com/iam](https://console.aws.amazon.com/iam) → **Users** → **Create user**
2. Username: `bartrix-backend`
3. Permissions: **Attach policies directly** → search `AmazonS3FullAccess` → attach
4. Create user

#### Generate Access Key
1. Open user `bartrix-backend` → **Security credentials** tab
2. **Create access key** → choose **Application running outside AWS**
3. Copy **Access key ID** and **Secret access key** (shown only once)

---

### 2. Code Changes

#### Added AWS SDK package
`src/Bartrix.Api/Bartrix.Api.csproj`:
```xml
<PackageReference Include="AWSSDK.S3" Version="3.7.414.1" />
```

#### New config class
`src/Bartrix.Api/Media/S3Options.cs` — holds bucket name, region, credentials, and an optional `ServiceUrl` override for local MinIO.

#### New storage implementation
`src/Bartrix.Api/Media/S3MediaStorage.cs` — implements `IMediaStorage`:
- Uploads with `CannedACL.PublicRead` so images are immediately accessible
- Returns public URL: `https://{bucket}.s3.{region}.amazonaws.com/{key}`
- In dev (when `ServiceUrl` is set): returns `http://localhost:9000/{bucket}/{key}`
- Supports `CreateBucketIfMissing` for local dev convenience

#### DI registration
`src/Bartrix.Api/DependencyInjection/ServiceCollectionExtensions.cs`:
- Removed `MinioOptions` + `IMinioClientFactory` + `MinioMediaStorage`
- Registered `S3Options` + `S3MediaStorage` instead

#### Config files updated

`appsettings.json` (base — no real credentials here):
```json
"Storage": {
  "S3": {
    "BucketName": "bartrix-media",
    "Region": "us-east-1",
    "AccessKey": "",
    "SecretKey": "",
    "ServiceUrl": "",
    "ForcePathStyle": false,
    "CreateBucketIfMissing": false
  }
}
```

`appsettings.Development.json` (points at local MinIO):
```json
"Storage": {
  "S3": {
    "BucketName": "bartrix-media-dev",
    "Region": "us-east-1",
    "AccessKey": "minioadmin",
    "SecretKey": "minioadmin",
    "ServiceUrl": "http://localhost:9000",
    "ForcePathStyle": true,
    "CreateBucketIfMissing": true
  }
}
```

---

### 3. Environment Variables (`.env`)

```
Storage__S3__BucketName=bartix-684365645541-eu-north-1-an
Storage__S3__Region=eu-north-1
Storage__S3__AccessKey=<your-access-key-id>
Storage__S3__SecretKey=<your-secret-access-key>
Storage__S3__ServiceUrl=
Storage__S3__ForcePathStyle=false
Storage__S3__CreateBucketIfMissing=false
```

`.env` is gitignored — never commit it.

---

## How It Works

```
Dev  →  MinIO (localhost:9000)  ←─ same S3MediaStorage ─→  S3 (eu-north-1)  ←  Prod
```

The single `S3MediaStorage` class handles both:
- **Dev:** `ServiceUrl=http://localhost:9000` + `ForcePathStyle=true` → talks to MinIO
- **Prod:** `ServiceUrl` is empty → talks to real AWS S3

Uploaded image URLs look like:
- Dev:  `http://localhost:9000/bartrix-media-dev/2026/06/abc123.jpg`
- Prod: `https://bartix-684365645541-eu-north-1-an.s3.eu-north-1.amazonaws.com/2026/06/abc123.jpg`

---

## Rotating Credentials

If the secret key is ever exposed, rotate it:
1. IAM → Users → `bartrix-backend` → Security credentials
2. Deactivate the old access key
3. Create a new access key
4. Update `.env` (and any deployed environment variables)

---

## Deployment Checklist

When deploying, set these as environment variables on your platform (Railway, Render, etc.):

| Variable | Value |
|----------|-------|
| `Storage__S3__BucketName` | `bartix-684365645541-eu-north-1-an` |
| `Storage__S3__Region` | `eu-north-1` |
| `Storage__S3__AccessKey` | your IAM access key ID |
| `Storage__S3__SecretKey` | your IAM secret access key |
| `Storage__S3__ServiceUrl` | *(leave empty)* |
| `Storage__S3__ForcePathStyle` | `false` |
