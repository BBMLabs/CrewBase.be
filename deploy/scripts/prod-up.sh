#!/usr/bin/env bash
# =============================================================================
# RowingClub - Production ortamini ayaga kaldirir.
# Repo kok dizininden calistirin: ./deploy/scripts/prod-up.sh
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

cd "${REPO_ROOT}"

if [ ! -f ".env.production" ]; then
  echo "Uyari: .env.production bulunamadi. Once '.env.example' dosyasini kopyalayip doldurun:"
  echo "  cp .env.example .env.production"
  exit 1
fi

ENV_FILE="../../.env.production" docker compose -f deploy/docker/docker-compose.yml --env-file .env.production up -d --build

echo "RowingClub production ortami ayaga kaldirildi."
echo "  API:        http://localhost:8080"
echo "  Prometheus: http://localhost:9090"
echo "  Grafana:    http://localhost:3000"
