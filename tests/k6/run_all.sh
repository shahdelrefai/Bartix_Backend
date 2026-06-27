#!/usr/bin/env bash
# Seed the DB then run smoke → load tests
set -e

BASE_URL="${BASE_URL:-http://localhost:5033}"
K6="${K6:-k6}"

echo "==> Seeding database at $BASE_URL"
curl -sf -X POST "$BASE_URL/api/seed/full" | python3 -m json.tool

echo ""
echo "==> Smoke test (1 VU x 1 iteration — all checks must pass)"
$K6 run -e BASE_URL="$BASE_URL" scenarios/smoke.js

echo ""
echo "==> Load test (50 VUs x 5 min)"
$K6 run -e BASE_URL="$BASE_URL" scenarios/load.js

echo ""
echo "All tests passed!"
