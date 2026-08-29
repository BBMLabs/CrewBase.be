#!/usr/bin/env bash
# =============================================================================
# RowingClub - Production ortamini durdurur.
# Repo kok dizininden calistirin: ./deploy/scripts/prod-down.sh
# =============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"

cd "${REPO_ROOT}"

ENV_FILE="../../.env.production" docker compose -f deploy/docker/docker-compose.yml --env-file .env.production down

echo "RowingClub production ortami durduruldu."
