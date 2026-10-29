# System Prompt Specification: Stream-of-Consciousness AI Learning Blog Platform

```markdown
# TASK SPECIFICATION
Build a full-stack, Dockerized stream-of-consciousness web blog post editor and static viewer system designed for documenting learning experiences with AI.

## KEY ARCHITECTURAL REQUIREMENTS
1. Architecture & Services (docker-compose.yml):
   - `backend`: C# .NET 8 Web API + Entity Framework Core (MySQL).
   - `db`: MySQL 8.0 container with persistent volumes.
   - `frontend`: Vue 3 SPA (Vite + Options API or Composition API + Tailwind CSS or Pinia).
   - `nginx`: Reverse proxy & static file server.
2. High-Speed Static Serving:
   - When a post is published/updated, the C# backend generates/compiles static HTML files (`/dist/posts/{id}.html`) directly into a shared Docker volume mounted to Nginx (`/usr/share/nginx/html/posts`).
   - Nginx handles incoming reader requests for posts directly from disk for ultra-fast response times.
3. Custom Dockerfiles:
   - Provide custom `Dockerfile` definitions for `backend`, `frontend`, and `nginx`, even if simple multi-stage builds.

---

## DATABASE SCHEME & DOMAIN MODELS (EF Core C#)

Implement the following C# Entity Framework Core entities:

```csharp
namespace BlogEngine.Core.Entities
{
    public enum MoveClassification
    {
        Brilliant,  // !!
        Great,      // !
        Best,       // ★
        Excellent,  // 👍
        Good,       // ✔
        Book,       // 📖
        Inaccuracy, // ?!
        Mistake,    // ?
        Blunder,    // ??
        Miss        // ❌
    }

    public enum SectionType
    {
        MarkdownText,
        LLMConversation,
        CodeSnippet,
        Image,
        Quote
    }

    public class BlogPost
    {
        public int Id { get; set; }
        public string Guid { get; set; } = System.Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;
        public bool IsPublished { get; set; } = false;

        // Versioning & Branching Mechanics
        public int? PreviousVersionId { get; set; }
        public BlogPost? PreviousVersion { get; set; }
        public int? NextVersionId { get; set; }
        public BlogPost? NextVersion { get; set; }
        
        // Root ID to track branches derived from the same base draft
        public int RootPostId { get; set; }

        // Dynamic Post Content Structure
        public List<PostSection> Sections { get; set; } = new();
        public List<ReaderReaction> Reactions { get; set; } = new();
    }

    public class PostSection
    {
        public int Id { get; set; }
        public int BlogPostId { get; set; }
        public int SortOrder { get; set; }
        public SectionType Type { get; set; }

        // Standard Content / Text / Image URL
        public string Content { get; set; } = string.Empty;

        // LLM Conversation Fields (Used when Type == LLMConversation)
        public string? Prompt { get; set; }
        public string? LlmOutput { get; set; }
        public string? Provider { get; set; } // e.g., ChatGPT, Claude, Gemini
        public string? ShareUrl { get; set; }
    }

    public class ReaderReaction
    {
        public int Id { get; set; }
        public int BlogPostId { get; set; }
        public int PostSectionId { get; set; }
        
        // Highlighted text selection
        public string HighlightedText { get; set; } = string.Empty;
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
        
        // Chess.com style commentary classification
        public MoveClassification Classification { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string ReaderName { get; set; } = "Anonymous";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

```

---

## BACKEND FEATURES & API ENDPOINTS

1. **LLM Share Importer Service**:
* Provide an endpoint `POST /api/posts/import-llm-share` accepting `{ url: string }`.
* Support parsing common public chat links (e.g., ChatGPT share links, Claude share URLs, Gemini share URLs) or fallback to a web-scraper/metadata extractor to pull the latest prompt/response pairs automatically.


2. **HTML Compiler Service**:
* Create `IStaticPageCompiler` in C#.
* Razor Light or Scriban template to render `BlogPost` into a self-contained static HTML page (`index.html` structure) containing the Chess.com reaction dynamic JS bundle.
* Saves static output directly to shared Docker volume directory: `/var/www/static_blogs/{id}.html`.


3. **API Endpoints**:
* `GET /api/posts`: Get all posts.
* `GET /api/posts/{id}`: Get post detail with sections and reactions.
* `POST /api/posts`: Create post draft.
* `PUT /api/posts/{id}`: Update post sections / create dynamic branch version.
* `POST /api/posts/{id}/restore/{versionId}`: Restore to a previous version.
* `POST /api/posts/{id}/compile`: Trigger manual static HTML generation.
* `POST /api/posts/{id}/reactions`: Add reader highlighted commentary reaction.



---

## FRONTEND SPECIFICATION (Vue 3)

### 1. Editor Page View (`/editor/:id?`)

* **Split Layout**: Left side = Inputs & Form Builder; Right side = Live Render Preview.
* **Header & Version Metadata**:
* Title Input.
* Author Input.
* `CreatedTime` (Disabled / Autofilled badge).
* Version Control Selectors: `Previous Version` dropdown and `Next Version / Branch` dropdown allowing instant switching, branch creation, or restoration.


* **Import LLM Share Bar**:
* URL Input + "Import Chat" button to fetch and instantly append extracted LLM Prompt/Response dynamic sections.


* **Dynamic Section Manager**:
* Add items: Buttons for `+ Add Text`, `+ Add LLM Pair`, `+ Add Image`, `+ Add Quote`.
* Reorder items: Up / Down arrow buttons or Drag-and-Drop handles on sections.
* **Editable Output**: LLM outputs must be completely editable in a rich textarea/markdown box by the user before publishing.


* **Right View (Live Preview)**:
* Real-time Markdown parsing (using `marked` or `markdown-it`).
* Chat Bubble styling for LLM User Prompt vs. LLM Output.



### 2. Post Viewer & Reaction Mechanism

* Render full static or preview HTML.
* **Chess.com Move Classification Sub-Section Highlight Mechanic**:
* When a reader highlights text in a post section, present a floating reaction tooltip popup.
* Reader selects a move classification tag:
* ‼️ **Brilliant** (Bright Teal)
* ❗ **Great** (Blue)
* ★ **Best** (Green)
* 👍 **Excellent** (Light Green)
* ✔ **Good** (Olive)
* 📖 **Book** (Brown/Gray)
* ⁉️ **Inaccuracy** (Yellow)
* ❓ **Mistake** (Orange)
* ❓❓ **Blunder** (Red)
* ❌ **Miss** (Dark Gray)


* Reader enters an optional comment refuting or analyzing the author's logic/LLM response.
* Display reaction badges inline or in a side gutter next to highlighted sub-sections.



---

## NGINX & DOCKER-COMPOSE CONFIGURATION

### Nginx Config (`nginx.conf`):

* Routing `/api/` -> Proxy pass to `http://backend:5000/`.
* Routing `/posts/` -> Static file serving directly from `/usr/share/nginx/html/posts/`.
* Routing `/` -> Serve Vue SPA frontend build.

### Docker-Compose (`docker-compose.yml`):

* Services: `db` (MySQL), `backend` (.NET API), `frontend` (Vue app build stage), `nginx` (Gateway & Static Host).
* Named volume `static_posts` shared between `backend` (write) and `nginx` (read-only).

```

---

### Instructions for execution:
1. Generate the file tree structure.
2. Implement the full `.cs` files for C# backend models, EF Core DbContext, controllers, and static renderer.
3. Provide the Vue 3 component code (`EditorView.vue` and `PostViewer.vue`).
4. Provide `docker-compose.yml`, custom `Dockerfile`s, and `nginx.conf`.

```
