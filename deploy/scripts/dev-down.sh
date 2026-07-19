#!/usr/bin/env bash
# =============================================================================
# RowingClub - Lokal gelistirme ortamini durdurur.
# Repo kok dizininden calistirin: ./deploy/scripts/dev-down.sh
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

cd "${REPO_ROOT}"

docker compose -f deploy/docker/docker-compose.yml --env-file .env.developer down

echo "RowingClub lokal gelistirme ortami durduruldu."
