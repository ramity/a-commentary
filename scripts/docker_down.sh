#!/usr/bin/env bash
# ========================================================
# Script: docker_down.sh
# Usage: ./scripts/docker_down.sh [dev|prod|all] [--volumes]
# Default mode: prod
# ========================================================

set -e

MODE="${1:-prod}"
shift || true
EXTRA_ARGS="$@"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [ "$MODE" = "dev" ] || [ "$MODE" = "development" ]; then
    echo "Stopping DEVELOPMENT containers..."
    docker compose -f docker-compose.dev.yml down $EXTRA_ARGS
elif [ "$MODE" = "all" ]; then
    echo "Stopping ALL containers (dev & prod)..."
    docker compose -f docker-compose.dev.yml down $EXTRA_ARGS 2>/dev/null || true
    docker compose -f docker-compose.yml down $EXTRA_ARGS 2>/dev/null || true
else
    echo "Stopping PRODUCTION containers..."
    docker compose -f docker-compose.yml down $EXTRA_ARGS
fi

echo "Containers successfully stopped."
