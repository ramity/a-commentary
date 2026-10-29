#!/usr/bin/env bash
# ========================================================
# Script: docker_up.sh
# Usage: ./scripts/docker_up.sh [dev|prod] [--build]
# Default mode: prod
# ========================================================

set -e

MODE="${1:-prod}"
shift || true
EXTRA_ARGS="$@"

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if [ "$MODE" = "dev" ] || [ "$MODE" = "development" ]; then
    echo "=========================================================="
    echo "Starting Stream of Consciousness in DEVELOPMENT mode..."
    echo "Backend: .NET 8 SDK + Hot Reload (dotnet watch)"
    echo "Frontend: Node 20 + Vite HMR"
    echo "Database: MySQL 8.0"
    echo "Proxy: Nginx Dev Gateway"
    echo "=========================================================="
    docker compose -f docker-compose.dev.yml up -d --build $EXTRA_ARGS
    echo ""
    echo "Services running:"
    echo "-> Web Application:   http://localhost/"
    echo "-> Vite Dev Server:   http://localhost:5173/"
    echo "-> Backend API:       http://localhost:5000/swagger"
    echo "-> Static Posts Disk: http://localhost/posts/{id}.html"
else
    echo "=========================================================="
    echo "Starting Stream of Consciousness in PRODUCTION mode..."
    echo "Backend: .NET 8 Compiled Release Runner (aspnet runtime)"
    echo "Frontend: Production Vite Static Build"
    echo "Database: MySQL 8.0"
    echo "Proxy: Nginx Gateway & Fast Disk Static Serving"
    echo "=========================================================="
    docker compose -f docker-compose.yml up -d --build $EXTRA_ARGS
    echo ""
    echo "Services running:"
    echo "-> Web Application:   http://localhost/"
    echo "-> Backend API:       http://localhost/api/"
    echo "-> Swagger UI:        http://localhost/swagger"
    echo "-> Static Posts Disk: http://localhost/posts/{id}.html"
fi
