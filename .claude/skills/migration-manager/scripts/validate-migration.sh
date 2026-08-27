#!/usr/bin/env bash
# Migration doğrulama: derler, bekleyen model değişikliği var mı bakar ve son migration'da
# tehlikeli operasyonları (DropTable/DropColumn) raporlar.
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/../../../.." && pwd)"
cd "$ROOT"

echo "== build =="
dotnet build RowingClub.sln --nologo -v q

check_context () {
  local ctx="$1" proj="$2" dir="$3"
  echo "== $ctx: bekleyen model değişikliği kontrolü =="
  if dotnet ef migrations has-pending-model-changes -p "$proj" -s src/RowingClub.Api --context "$ctx" 2>/dev/null; then
    echo "UYARI: $ctx için migration'a yansımamış model değişikliği var!"
  fi

  local last
  last=$(ls "$dir"/*_*.cs 2>/dev/null | grep -v Designer | sort | tail -1 || true)
  if [ -n "$last" ]; then
    echo "== $ctx: son migration: $(basename "$last") =="
    if grep -nE "Drop(Table|Column)" "$last"; then
      echo "UYARI: Son migration'da Drop operasyonu var - veri kaybını bilinçli onayla."
    else
      echo "Drop operasyonu yok."
    fi
  fi
}

check_context RowingClubDbContext \
  src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure \
  src/BuildingBlocks/RowingClub.BuildingBlocks.Infrastructure/Postgres/Migrations

check_context TenantDbContext \
  src/Modules/Scheduling/RowingClub.Scheduling.Infrastructure \
  src/Modules/Scheduling/RowingClub.Scheduling.Infrastructure/Persistence/Migrations

echo "== tamam =="
