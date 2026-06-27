# Bartrix k6 Test Suite

## Prerequisites
- [k6](https://grafana.com/docs/k6/latest/get-started/installation/) installed
- Backend running at `http://localhost:5033` (or set `$BASE_URL`)
- PostgreSQL seeder endpoint available (`/api/seed/full`)

## Quick Start

```bash
# 1. Start the backend
cd ../../src
~/.dotnet/dotnet run --project Bartrix.Api/

# 2. (New terminal) Run all tests
cd tests/k6
./run_all.sh
```

## Individual Scripts

| Script | Purpose |
|--------|---------|
| `scenarios/smoke.js` | 1 VU × 1 iteration — CI gate, all checks must pass |
| `scenarios/load.js`  | 50 VUs, 5 min — baseline performance |
| `scenarios/stress.js`| Ramp to 200 VUs — find breaking point |

```bash
# Smoke only
k6 run scenarios/smoke.js

# Load with custom base URL
BASE_URL=http://prod.example.com k6 run scenarios/load.js

# Single flow
k6 run flows/09_search.js
```

## Flow Coverage

| Flow | Endpoints tested |
|------|-----------------|
| 01_auth | register, login, email OTP, password reset, premium, logout |
| 02_listings | CRUD, AI suggest, favorites |
| 03_trades | create proposal, accept |
| 04_messaging | list conversations, send message |
| 05_payments | create, webhook simulation |
| 06_wallet | balance, transactions, withdrawal |
| 07_notifications | list, unread count, mark all read |
| 08_admin | user search, stats, listing management |
| 09_search | text, category, price range, condition, sort, pagination |
| 10_reputation | user reputation, reviews |
