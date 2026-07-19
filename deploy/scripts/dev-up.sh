#!/usr/bin/env bash
# =============================================================================
# RowingClub - Lokal gelistirme ortamini ayaga kaldirir.
# Repo kok dizininden calistirin: ./deploy/scripts/dev-up.sh
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

cd "${REPO_ROOT}"

if [ ! -f ".env.developer" ]; then
  echo "Uyari: .env.developer bulunamadi. Once '.env.example' dosyasini kopyalayip doldurun:"
  echo "  cp .env.example .env.developer"
  exit 1
fi

docker compose -f deploy/docker/docker-compose.yml --env-file .env.developer up -d

echo "RowingClub lokal gelistirme ortami ayaga kaldirildi."
echo "  API:        http://localhost:8080"
echo "  Prometheus: http://localhost:9090"
echo "  Grafana:    http://localhost:3000"
