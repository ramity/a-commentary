# Stream-of-Consciousness AI Learning Blog Platform

A full-stack, Dockerized stream-of-consciousness web blog editor and static viewer system designed specifically for documenting learning experiences and pair programming sessions with AI.

Includes dynamic versioning & branching, public LLM share importing (ChatGPT, Claude, Gemini), high-speed static compilation served directly from disk via Nginx, and a **Chess.com-style commentary engine** for annotating posts and AI outputs with 10 move classifications.

---

## 🏛️ Architecture Overview

```mermaid
graph TD
    Client[Browser / Reader] -->|HTTP /posts/{id}.html| Nginx[Nginx Reverse Proxy & Static Host]
    Client -->|HTTP /api/...| Nginx
    Client -->|HTTP / (SPA)| Nginx
    
    subgraph Docker Network
        Nginx -->|Fast Disk Reads /usr/share/nginx/html/posts| Vol[(Named Volume: static_posts)]
        Nginx -->|Proxy Pass :5000| Backend[C# .NET 8 Web API]
        Backend -->|Compiles HTML /var/www/static_blogs| Vol
        Backend -->|EF Core MySQL| DB[(MySQL 8.0 Container)]
    end
```

### Key Components

1. **Backend (`backend/`)**:
   - C# .NET 8 Web API + Entity Framework Core (`Pomelo.EntityFrameworkCore.MySql`).
   - `IStaticPageCompiler`: Compiles `BlogPost` entities into standalone HTML files (`/var/www/static_blogs/{id}.html`) embedding responsive styling, rendered markdown, existing annotations, and interactive reader reaction scripts.
   - `ILlmShareImporter`: Ingests and extracts prompt/response pairs from ChatGPT, Claude, and Gemini share URLs or falls back to web scraping.
   - Automatic database migration and seed data on startup.

2. **Frontend (`frontend/`)**:
   - Vue 3 SPA with Vite, Composition API, and Tailwind CSS.
   - **Split-View Editor** (`/editor/:id?`):
     - Dynamic section manager (`+ Add Text`, `+ Add LLM Pair`, `+ Add Image`, `+ Add Quote`, `+ Add Code`).
     - Reordering and editable LLM outputs.
     - Version branching controls (`Fork Branch`, `Restore Version`).
     - Import LLM Share bar.
     - Real-time markdown preview with speech-bubble styling.
   - **Interactive Reader & Commentary** (`/posts/:id`):
     - Text selection floating tooltip for Chess.com move classifications:
       - ‼️ **Brilliant** (`#1baca6`)
       - ❗ **Great** (`#3692e7`)
       - ★ **Best** (`#7fa650`)
       - 👍 **Excellent** (`#98bc64`)
       - ✔ **Good** (`#a7bd86`)
       - 📖 **Book** (`#a88865`)
       - ⁉️ **Inaccuracy** (`#e58f2a`)
       - ❓ **Mistake** (`#e6912c`)
       - ❓❓ **Blunder** (`#ca3431`)
       - ❌ **Miss** (`#e06453`)
     - Slide-over commentary drawer for inspecting all reader critiques.

3. **High-Speed Static Serving (`nginx/`)**:
   - Nginx serves compiled HTML directly from `/usr/share/nginx/html/posts/{id}.html` via a shared volume without hitting the backend API or database.
   - Reverse proxies `/api/` to the .NET 8 backend.
   - Serves the Vue 3 SPA on `/`.

---

## 🚀 Quick Start (Development & Production)

Convenient scripts are provided in the `scripts/` directory for both Linux/macOS (`.sh`) and Windows (`.ps1`).

### Production Mode (Maximum Performance)
Builds the .NET API in Release mode with the minimal `aspnet:8.0` runtime image and builds static Vue SPA assets into Nginx.

```bash
# Linux / macOS
./scripts/docker_up.sh prod

# Windows PowerShell
.\scripts\docker_up.ps1 prod
```

Or directly via Docker Compose:
```bash
docker compose -f docker-compose.yml up -d --build
```

### Development Mode (SDK + Hot Reloading)
Runs .NET 8 SDK with `dotnet watch` and Vue with Vite HMR for live reloading while editing files locally.

```bash
# Linux / macOS
./scripts/docker_up.sh dev

# Windows PowerShell
.\scripts\docker_up.ps1 dev
```

Or directly via Docker Compose:
```bash
docker compose -f docker-compose.dev.yml up -d --build
```

### Stopping Containers

```bash
# Linux / macOS
./scripts/docker_down.sh prod   # or dev, or all

# Windows PowerShell
.\scripts\docker_down.ps1 prod  # or dev, or all
```

---

## 🌐 URLs & Service Endpoints

| Service | URL | Description |
| :--- | :--- | :--- |
| **Web Application (SPA)** | `http://localhost/` | Posts directory, editor, reader view |
| **High-Speed Static Posts** | `http://localhost/posts/{id}.html` | Static HTML served directly from disk by Nginx |
| **Swagger API Docs** | `http://localhost/swagger` or `http://localhost:5000/swagger` | Interactive REST API testing |
| **Backend Health Check** | `http://localhost/api/posts` | Post summaries and metadata |
| **Vite Dev Server** (Dev only) | `http://localhost:5173/` | Hot Module Replacement frontend |

---

## 📡 REST API Reference

- `GET /api/posts`: List all posts with summary counts and static links.
- `GET /api/posts/{id}`: Detailed post with ordered sections and reader reactions.
- `GET /api/posts/{id}/versions`: Get all related version and branch nodes under the same `RootPostId`.
- `POST /api/posts`: Create a new stream-of-consciousness post draft.
- `PUT /api/posts/{id}`: Update post sections or create a dynamic branch version (`createBranch: true`).
- `POST /api/posts/{id}/restore/{versionId}`: Restore post state from a previous version.
- `POST /api/posts/{id}/compile`: Manually trigger static HTML compilation to `/var/www/static_blogs/{id}.html`.
- `POST /api/posts/{id}/reactions`: Submit a reader highlighted commentary reaction with Chess.com classification.
- `POST /api/posts/import-llm-share`: `{ url: string }` imports prompt/response pairs from ChatGPT, Claude, or Gemini public links.
