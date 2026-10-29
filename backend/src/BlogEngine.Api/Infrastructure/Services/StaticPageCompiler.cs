using System.Text;
using System.Text.Encodings.Web;
using BlogEngine.Core.Entities;
using BlogEngine.Core.Interfaces;
using Markdig;

namespace BlogEngine.Infrastructure.Services
{
    public class StaticPageCompiler : IStaticPageCompiler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StaticPageCompiler> _logger;
        private readonly MarkdownPipeline _markdownPipeline;

        public StaticPageCompiler(IConfiguration configuration, ILogger<StaticPageCompiler> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _markdownPipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();
        }

        public async Task<string> CompileAndSaveAsync(BlogPost post)
        {
            var outputDirectory = _configuration["StaticPostsPath"] ?? "/var/www/static_blogs";
            
            try
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }

                var htmlContent = RenderToHtml(post);
                var filePath = Path.Combine(outputDirectory, $"{post.Id}.html");

                await File.WriteAllTextAsync(filePath, htmlContent, Encoding.UTF8);
                _logger.LogInformation("Successfully compiled static HTML for Post ID {Id} to {Path}", post.Id, filePath);

                return filePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compile static HTML for Post ID {Id}", post.Id);
                throw;
            }
        }

        public string RenderToHtml(BlogPost post)
        {
            var orderedSections = post.Sections.OrderBy(s => s.SortOrder).ToList();
            var reactions = post.Reactions.OrderByDescending(r => r.CreatedAt).ToList();

            var sectionsHtml = new StringBuilder();
            foreach (var section in orderedSections)
            {
                var sectionReactions = reactions.Where(r => r.PostSectionId == section.Id).ToList();
                sectionsHtml.AppendLine(RenderSection(section, sectionReactions));
            }

            var reactionsSidebarHtml = RenderReactionsSidebar(reactions);

            return $$"""
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>{{HtmlEncoder.Default.Encode(post.Title)}} - Stream of Consciousness</title>
  <meta name="author" content="{{HtmlEncoder.Default.Encode(post.Author)}}">
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
  <link href="https://fonts.googleapis.com/css2?family=JetBrains+Mono:wght@400;500;700&family=Plus+Jakarta+Sans:ital,wght@0,300;0,400;0,500;0,600;0,700;1,400&display=swap" rel="stylesheet">
  <style>
    :root {
      --bg: #0b0f19;
      --card-bg: #111827;
      --card-border: #1f2937;
      --text: #f3f4f6;
      --text-muted: #9ca3af;
      --accent: #38bdf8;
      --accent-glow: rgba(56, 189, 248, 0.2);
      
      /* Chess.com Move Classification Colors */
      --color-brilliant: #1baca6;
      --color-great: #3692e7;
      --color-best: #7fa650;
      --color-excellent: #98bc64;
      --color-good: #a7bd86;
      --color-book: #a88865;
      --color-inaccuracy: #e58f2a;
      --color-mistake: #e6912c;
      --color-blunder: #ca3431;
      --color-miss: #e06453;
    }

    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      background-color: var(--bg);
      color: var(--text);
      font-family: 'Plus Jakarta Sans', system-ui, -apple-system, sans-serif;
      line-height: 1.7;
      padding: 0;
      margin: 0;
      overflow-x: hidden;
    }

    .container {
      max-width: 900px;
      margin: 0 auto;
      padding: 2.5rem 1.5rem 6rem;
    }

    /* Header & Meta */
    header {
      margin-bottom: 3rem;
      padding-bottom: 2rem;
      border-bottom: 1px solid var(--card-border);
    }
    .badge-bar {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      margin-bottom: 1rem;
      flex-wrap: wrap;
    }
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
      font-size: 0.75rem;
      font-weight: 600;
      padding: 0.25rem 0.65rem;
      border-radius: 9999px;
      text-transform: uppercase;
      letter-spacing: 0.05em;
    }
    .badge-published {
      background: rgba(34, 197, 94, 0.15);
      color: #4ade80;
      border: 1px solid rgba(34, 197, 94, 0.3);
    }
    .badge-version {
      background: rgba(147, 51, 234, 0.15);
      color: #c084fc;
      border: 1px solid rgba(147, 51, 234, 0.3);
    }
    .badge-static {
      background: rgba(56, 189, 248, 0.15);
      color: #38bdf8;
      border: 1px solid rgba(56, 189, 248, 0.3);
    }
    h1 {
      font-size: 2.5rem;
      font-weight: 700;
      line-height: 1.25;
      letter-spacing: -0.025em;
      margin-bottom: 1rem;
      background: linear-gradient(135deg, #ffffff 0%, #cbd5e1 100%);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }
    .meta {
      display: flex;
      align-items: center;
      gap: 1.5rem;
      font-size: 0.875rem;
      color: var(--text-muted);
    }
    .meta span strong {
      color: #e2e8f0;
    }

    /* Section Containers */
    .section-wrapper {
      position: relative;
      margin-bottom: 2.5rem;
      transition: all 0.2s ease;
    }
    .section-content {
      position: relative;
    }

    /* Markdown prose */
    .prose {
      font-size: 1.05rem;
      color: #e2e8f0;
    }
    .prose h1, .prose h2, .prose h3 {
      color: #ffffff;
      margin-top: 1.75rem;
      margin-bottom: 0.75rem;
      font-weight: 700;
    }
    .prose p { margin-bottom: 1.25rem; }
    .prose ul, .prose ol { margin-left: 1.5rem; margin-bottom: 1.25rem; }
    .prose li { margin-bottom: 0.35rem; }
    .prose a { color: var(--accent); text-decoration: underline; }

    /* LLM Conversation Bubbles */
    .llm-card {
      background: var(--card-bg);
      border: 1px solid var(--card-border);
      border-radius: 1rem;
      padding: 1.5rem;
      box-shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.4);
    }
    .llm-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 1.25rem;
      padding-bottom: 0.75rem;
      border-bottom: 1px solid rgba(255, 255, 255, 0.05);
    }
    .llm-provider-tag {
      font-size: 0.8rem;
      font-weight: 600;
      color: #38bdf8;
      background: rgba(56, 189, 248, 0.1);
      padding: 0.2rem 0.6rem;
      border-radius: 0.375rem;
      display: inline-flex;
      align-items: center;
      gap: 0.35rem;
    }
    .llm-share-link {
      font-size: 0.75rem;
      color: var(--text-muted);
      text-decoration: none;
    }
    .llm-share-link:hover { color: var(--text); }
    .chat-bubble {
      border-radius: 0.75rem;
      padding: 1.25rem;
      margin-bottom: 1rem;
    }
    .user-bubble {
      background: #1e293b;
      border: 1px solid #334155;
      margin-left: 1.5rem;
    }
    .user-bubble .speaker {
      font-size: 0.75rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #94a3b8;
      margin-bottom: 0.5rem;
    }
    .ai-bubble {
      background: #0f172a;
      border: 1px solid #1e293b;
      border-left: 3px solid #38bdf8;
      margin-right: 1.5rem;
    }
    .ai-bubble .speaker {
      font-size: 0.75rem;
      font-weight: 700;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      color: #38bdf8;
      margin-bottom: 0.5rem;
    }

    /* Code block */
    pre code {
      display: block;
      background: #0d1117;
      border: 1px solid #21262d;
      border-radius: 0.5rem;
      padding: 1.25rem;
      font-family: 'JetBrains Mono', monospace;
      font-size: 0.9rem;
      color: #58a6ff;
      overflow-x: auto;
      line-height: 1.5;
    }

    /* Quotes */
    blockquote {
      border-left: 4px solid var(--accent);
      padding: 1rem 1.5rem;
      background: rgba(56, 189, 248, 0.05);
      border-radius: 0 0.5rem 0.5rem 0;
      font-style: italic;
      color: #cbd5e1;
      margin: 1.5rem 0;
    }

    /* Images */
    .post-image {
      width: 100%;
      border-radius: 0.75rem;
      border: 1px solid var(--card-border);
      max-height: 500px;
      object-fit: cover;
    }

    /* Chess Move Classification Badge */
    .chess-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.3rem;
      font-size: 0.75rem;
      font-weight: 700;
      padding: 0.2rem 0.5rem;
      border-radius: 0.375rem;
      color: #ffffff;
      white-space: nowrap;
      vertical-align: middle;
      cursor: pointer;
    }
    .chess-badge-brilliant { background-color: var(--color-brilliant); }
    .chess-badge-great { background-color: var(--color-great); }
    .chess-badge-best { background-color: var(--color-best); }
    .chess-badge-excellent { background-color: var(--color-excellent); }
    .chess-badge-good { background-color: var(--color-good); }
    .chess-badge-book { background-color: var(--color-book); }
    .chess-badge-inaccuracy { background-color: var(--color-inaccuracy); }
    .chess-badge-mistake { background-color: var(--color-mistake); }
    .chess-badge-blunder { background-color: var(--color-blunder); }
    .chess-badge-miss { background-color: var(--color-miss); }

    /* Inline Highlighted Text */
    .reaction-highlight {
      background-color: rgba(230, 145, 44, 0.25);
      border-bottom: 2px dashed #e6912c;
      padding: 0.1rem 0.2rem;
      border-radius: 0.2rem;
      cursor: pointer;
      position: relative;
    }
    .reaction-highlight:hover {
      background-color: rgba(230, 145, 44, 0.45);
    }

    /* Floating Reaction Trigger Tooltip */
    #reaction-tooltip {
      position: absolute;
      display: none;
      z-index: 1000;
      background: #1e293b;
      border: 1px solid #334155;
      border-radius: 0.75rem;
      box-shadow: 0 20px 30px -10px rgba(0, 0, 0, 0.7);
      padding: 0.75rem;
      width: 320px;
    }
    #reaction-tooltip h4 {
      font-size: 0.8rem;
      color: #94a3b8;
      text-transform: uppercase;
      letter-spacing: 0.05em;
      margin-bottom: 0.5rem;
    }
    .classification-grid {
      display: grid;
      grid-template-columns: repeat(5, 1fr);
      gap: 0.4rem;
      margin-bottom: 0.75rem;
    }
    .class-btn {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      background: #0f172a;
      border: 1px solid #334155;
      border-radius: 0.375rem;
      padding: 0.35rem 0.2rem;
      cursor: pointer;
      font-size: 0.7rem;
      color: #e2e8f0;
      transition: all 0.15s ease;
    }
    .class-btn:hover, .class-btn.selected {
      border-color: #38bdf8;
      background: #1e293b;
      transform: translateY(-1px);
    }
    .class-btn .icon { font-size: 1rem; margin-bottom: 0.15rem; }
    
    .comment-input, .name-input {
      width: 100%;
      background: #0f172a;
      border: 1px solid #334155;
      color: #fff;
      font-size: 0.8rem;
      padding: 0.4rem 0.6rem;
      border-radius: 0.375rem;
      margin-bottom: 0.5rem;
      font-family: inherit;
    }
    .submit-btn {
      width: 100%;
      background: #2563eb;
      color: white;
      border: none;
      font-size: 0.8rem;
      font-weight: 600;
      padding: 0.5rem;
      border-radius: 0.375rem;
      cursor: pointer;
    }
    .submit-btn:hover { background: #1d4ed8; }

    /* Reactions Sidebar */
    .sidebar-toggle-btn {
      position: fixed;
      bottom: 2rem;
      right: 2rem;
      background: #2563eb;
      color: white;
      border: none;
      border-radius: 9999px;
      padding: 0.75rem 1.25rem;
      font-size: 0.875rem;
      font-weight: 600;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      box-shadow: 0 10px 20px rgba(0,0,0,0.4);
      cursor: pointer;
      z-index: 900;
    }
    .sidebar {
      position: fixed;
      top: 0;
      right: -420px;
      width: 400px;
      height: 100vh;
      background: #111827;
      border-left: 1px solid #1f2937;
      box-shadow: -10px 0 30px rgba(0,0,0,0.5);
      z-index: 1050;
      padding: 1.5rem;
      overflow-y: auto;
      transition: right 0.3s cubic-bezier(0.16, 1, 0.3, 1);
    }
    .sidebar.open { right: 0; }
    .sidebar-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-bottom: 1rem;
      border-bottom: 1px solid #1f2937;
      margin-bottom: 1.25rem;
    }
    .close-btn {
      background: none;
      border: none;
      color: #9ca3af;
      font-size: 1.5rem;
      cursor: pointer;
    }
    .reaction-item {
      background: #1e293b;
      border: 1px solid #334155;
      border-radius: 0.5rem;
      padding: 0.85rem;
      margin-bottom: 0.85rem;
    }
    .reaction-item-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-bottom: 0.5rem;
    }
    .reaction-quote {
      font-size: 0.8rem;
      color: #94a3b8;
      border-left: 2px solid #64748b;
      padding-left: 0.5rem;
      margin-bottom: 0.5rem;
      font-style: italic;
    }
    .reaction-comment {
      font-size: 0.85rem;
      color: #f1f5f9;
    }
    .reaction-reader {
      font-size: 0.75rem;
      color: #64748b;
      margin-top: 0.35rem;
    }
  </style>
</head>
<body>

  <div class="container">
    <header>
      <div class="badge-bar">
        <span class="badge badge-published">● Published Static</span>
        <span class="badge badge-version">Root ID: #{{post.RootPostId}}</span>
        <span class="badge badge-static">Fast Disk Serving</span>
        <a href="/editor/{{post.Id}}" style="margin-left: auto; color: var(--accent); font-size: 0.85rem; text-decoration: none;">✏️ Open in Editor</a>
      </div>

      <h1>{{HtmlEncoder.Default.Encode(post.Title)}}</h1>

      <div class="meta">
        <span>By <strong>{{HtmlEncoder.Default.Encode(post.Author)}}</strong></span>
        <span>Created <strong>{{post.CreatedTime:yyyy-MM-dd HH:mm}} UTC</strong></span>
        <span>Updated <strong>{{post.UpdatedTime:yyyy-MM-dd HH:mm}} UTC</strong></span>
      </div>
    </header>

    <main id="post-content">
      {{sectionsHtml}}
    </main>
  </div>

  <!-- Floating Text Selection Commentary Tooltip -->
  <div id="reaction-tooltip">
    <h4>Chess.com Move Classification</h4>
    <div class="classification-grid" id="classification-picker">
      <button type="button" class="class-btn selected" data-class="Brilliant" title="Brilliant (!!)"><span class="icon" style="color:var(--color-brilliant)">‼️</span>Brilliant</button>
      <button type="button" class="class-btn" data-class="Great" title="Great (!)"><span class="icon" style="color:var(--color-great)">❗</span>Great</button>
      <button type="button" class="class-btn" data-class="Best" title="Best (★)"><span class="icon" style="color:var(--color-best)">★</span>Best</button>
      <button type="button" class="class-btn" data-class="Excellent" title="Excellent (👍)"><span class="icon" style="color:var(--color-excellent)">👍</span>Excl</button>
      <button type="button" class="class-btn" data-class="Good" title="Good (✔)"><span class="icon" style="color:var(--color-good)">✔</span>Good</button>
      <button type="button" class="class-btn" data-class="Book" title="Book (📖)"><span class="icon" style="color:var(--color-book)">📖</span>Book</button>
      <button type="button" class="class-btn" data-class="Inaccuracy" title="Inaccuracy (?!);"><span class="icon" style="color:var(--color-inaccuracy)">⁉️</span>Inacc</button>
      <button type="button" class="class-btn" data-class="Mistake" title="Mistake (?)"><span class="icon" style="color:var(--color-mistake)">❓</span>Mistake</button>
      <button type="button" class="class-btn" data-class="Blunder" title="Blunder (??)"><span class="icon" style="color:var(--color-blunder)">❓❓</span>Blunder</button>
      <button type="button" class="class-btn" data-class="Miss" title="Miss (❌)"><span class="icon" style="color:var(--color-miss)">❌</span>Miss</button>
    </div>
    <input type="text" id="reader-name" class="name-input" placeholder="Your name (e.g. Grandmaster Bob)" />
    <textarea id="reader-comment" class="comment-input" rows="2" placeholder="Analyze/refute this move..."></textarea>
    <button type="button" class="submit-btn" id="submit-reaction-btn">Post Move Commentary</button>
  </div>

  <!-- Sidebar Toggle Button -->
  <button class="sidebar-toggle-btn" id="sidebar-toggle-btn">
    <span>♟️ Commentary</span>
    <span style="background: rgba(255,255,255,0.2); padding: 0.1rem 0.5rem; border-radius: 999px;">{{reactions.Count}}</span>
  </button>

  <!-- Sidebar Drawer -->
  <div class="sidebar" id="commentary-sidebar">
    <div class="sidebar-header">
      <h3 style="font-size: 1.1rem; color: #fff;">Reader Move Analysis</h3>
      <button class="close-btn" id="close-sidebar-btn">&times;</button>
    </div>
    <div id="sidebar-reactions-list">
      {{reactionsSidebarHtml}}
    </div>
  </div>

  <script>
    (function() {
      const postId = {{post.Id}};
      let selectedText = '';
      let targetSectionId = null;
      let startOffset = 0;
      let endOffset = 0;
      let selectedClassification = 'Brilliant';

      const tooltip = document.getElementById('reaction-tooltip');
      const classButtons = document.querySelectorAll('.class-btn');
      const submitBtn = document.getElementById('submit-reaction-btn');
      const readerNameInput = document.getElementById('reader-name');
      const readerCommentInput = document.getElementById('reader-comment');
      const sidebarToggleBtn = document.getElementById('sidebar-toggle-btn');
      const closeSidebarBtn = document.getElementById('close-sidebar-btn');
      const sidebar = document.getElementById('commentary-sidebar');

      // Classification selector
      classButtons.forEach(btn => {
        btn.addEventListener('click', (e) => {
          e.stopPropagation();
          classButtons.forEach(b => b.classList.remove('selected'));
          btn.classList.add('selected');
          selectedClassification = btn.getAttribute('data-class');
        });
      });

      // Sidebar open/close
      sidebarToggleBtn.addEventListener('click', () => sidebar.classList.add('open'));
      closeSidebarBtn.addEventListener('click', () => sidebar.classList.remove('open'));

      // Listen for text selections on sections
      document.addEventListener('mouseup', function(e) {
        if (tooltip.contains(e.target) || sidebar.contains(e.target)) return;

        const selection = window.getSelection();
        const text = selection ? selection.toString().trim() : '';

        if (text && text.length >= 2) {
          const range = selection.getRangeAt(0);
          const commonAncestor = range.commonAncestorContainer;
          const sectionEl = commonAncestor.nodeType === 1 
            ? commonAncestor.closest('.section-wrapper') 
            : commonAncestor.parentElement.closest('.section-wrapper');

          if (sectionEl) {
            targetSectionId = parseInt(sectionEl.getAttribute('data-section-id'), 10);
            selectedText = text;
            startOffset = range.startOffset;
            endOffset = range.endOffset;

            const rect = range.getBoundingClientRect();
            tooltip.style.top = (window.scrollY + rect.bottom + 8) + 'px';
            tooltip.style.left = Math.max(16, Math.min(window.innerWidth - 340, (window.scrollX + rect.left))) + 'px';
            tooltip.style.display = 'block';
            return;
          }
        }

        // Hide if clicked elsewhere and no text selected
        if (!text) {
          tooltip.style.display = 'none';
        }
      });

      // Submit reaction
      submitBtn.addEventListener('click', async () => {
        if (!selectedText || !targetSectionId) return;

        submitBtn.disabled = true;
        submitBtn.innerText = 'Submitting...';

        try {
          const payload = {
            postSectionId: targetSectionId,
            highlightedText: selectedText,
            startOffset: startOffset,
            endOffset: endOffset,
            classification: selectedClassification,
            comment: readerCommentInput.value.trim(),
            readerName: readerNameInput.value.trim() || 'Anonymous Reader'
          };

          const res = await fetch(`/api/posts/${postId}/reactions`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
          });

          if (res.ok) {
            tooltip.style.display = 'none';
            readerCommentInput.value = '';
            // Reload page to display compiled reaction
            window.location.reload();
          } else {
            alert('Could not submit commentary reaction.');
          }
        } catch (err) {
          console.error(err);
          alert('Network error submitting reaction.');
        } finally {
          submitBtn.disabled = false;
          submitBtn.innerText = 'Post Move Commentary';
        }
      });
    })();
  </script>
</body>
</html>
""";
        }

        private string RenderSection(PostSection section, List<ReaderReaction> reactions)
        {
            var sectionId = section.Id;
            var reactionsBadgeHtml = new StringBuilder();

            foreach (var r in reactions)
            {
                var (badgeClass, symbol) = GetClassificationBadgeInfo(r.Classification);
                reactionsBadgeHtml.Append($"""
                    <span class="chess-badge {badgeClass}" title="{HtmlEncoder.Default.Encode(r.ReaderName)}: {HtmlEncoder.Default.Encode(r.Comment)}">
                        {symbol} {r.Classification}
                    </span>
                """);
            }

            var contentHtml = section.Type switch
            {
                SectionType.MarkdownText => $"""
                    <div class="prose">
                        {Markdown.ToHtml(section.Content ?? string.Empty, _markdownPipeline)}
                    </div>
                """,
                SectionType.LLMConversation => $"""
                    <div class="llm-card">
                        <div class="llm-header">
                            <span class="llm-provider-tag">
                                🤖 {HtmlEncoder.Default.Encode(section.Provider ?? "AI Assistant")}
                            </span>
                            {(string.IsNullOrWhiteSpace(section.ShareUrl) ? "" : $"<a href=\"{HtmlEncoder.Default.Encode(section.ShareUrl)}\" target=\"_blank\" class=\"llm-share-link\">🔗 View Share Source</a>")}
                        </div>
                        <div class="chat-bubble user-bubble">
                            <div class="speaker">User Prompt</div>
                            <div class="bubble-content">{Markdown.ToHtml(section.Prompt ?? string.Empty, _markdownPipeline)}</div>
                        </div>
                        <div class="chat-bubble ai-bubble">
                            <div class="speaker">{HtmlEncoder.Default.Encode(section.Provider ?? "AI Output")}</div>
                            <div class="bubble-content">{Markdown.ToHtml(section.LlmOutput ?? string.Empty, _markdownPipeline)}</div>
                        </div>
                    </div>
                """,
                SectionType.CodeSnippet => $"""
                    <pre><code>{HtmlEncoder.Default.Encode(section.Content ?? string.Empty)}</code></pre>
                """,
                SectionType.Quote => $"""
                    <blockquote>{Markdown.ToHtml(section.Content ?? string.Empty, _markdownPipeline)}</blockquote>
                """,
                SectionType.Image => $"""
                    <div style="text-align: center; margin: 1.5rem 0;">
                        <img src="{HtmlEncoder.Default.Encode(section.Content ?? string.Empty)}" alt="Post image" class="post-image" />
                    </div>
                """,
                _ => Markdown.ToHtml(section.Content ?? string.Empty, _markdownPipeline)
            };

            return $"""
                <section class="section-wrapper" data-section-id="{sectionId}">
                    {(reactionsBadgeHtml.Length > 0 ? $"<div style=\"display:flex; gap:0.4rem; margin-bottom:0.5rem; flex-wrap:wrap;\">{reactionsBadgeHtml}</div>" : "")}
                    <div class="section-content">
                        {contentHtml}
                    </div>
                </section>
            """;
        }

        private string RenderReactionsSidebar(List<ReaderReaction> reactions)
        {
            if (reactions.Count == 0)
            {
                return "<p style=\"color: #64748b; font-size: 0.9rem;\">No commentary moves yet. Select text in any section to post the first move analysis!</p>";
            }

            var sb = new StringBuilder();
            foreach (var r in reactions)
            {
                var (badgeClass, symbol) = GetClassificationBadgeInfo(r.Classification);
                sb.Append($"""
                    <div class="reaction-item">
                        <div class="reaction-item-header">
                            <span class="chess-badge {badgeClass}">{symbol} {r.Classification}</span>
                            <span style="font-size:0.75rem; color:#64748b;">{r.CreatedAt:MMM dd, HH:mm}</span>
                        </div>
                        <div class="reaction-quote">"{HtmlEncoder.Default.Encode(r.HighlightedText)}"</div>
                        {(string.IsNullOrWhiteSpace(r.Comment) ? "" : $"<div class=\"reaction-comment\">{HtmlEncoder.Default.Encode(r.Comment)}</div>")}
                        <div class="reaction-reader">— {HtmlEncoder.Default.Encode(r.ReaderName)}</div>
                    </div>
                """);
            }
            return sb.ToString();
        }

        private static (string badgeClass, string symbol) GetClassificationBadgeInfo(MoveClassification classification)
        {
            return classification switch
            {
                MoveClassification.Brilliant => ("chess-badge-brilliant", "‼️"),
                MoveClassification.Great => ("chess-badge-great", "❗"),
                MoveClassification.Best => ("chess-badge-best", "★"),
                MoveClassification.Excellent => ("chess-badge-excellent", "👍"),
                MoveClassification.Good => ("chess-badge-good", "✔"),
                MoveClassification.Book => ("chess-badge-book", "📖"),
                MoveClassification.Inaccuracy => ("chess-badge-inaccuracy", "⁉️"),
                MoveClassification.Mistake => ("chess-badge-mistake", "❓"),
                MoveClassification.Blunder => ("chess-badge-blunder", "❓❓"),
                MoveClassification.Miss => ("chess-badge-miss", "❌"),
                _ => ("chess-badge-good", "✔")
            };
        }
    }
}
