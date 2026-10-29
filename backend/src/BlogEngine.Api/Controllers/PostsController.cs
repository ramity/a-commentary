using BlogEngine.Core.DTOs;
using BlogEngine.Core.Entities;
using BlogEngine.Core.Interfaces;
using BlogEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BlogEngine.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PostsController : ControllerBase
    {
        private readonly BlogDbContext _context;
        private readonly IStaticPageCompiler _pageCompiler;
        private readonly ILlmShareImporter _llmImporter;
        private readonly ILogger<PostsController> _logger;

        public PostsController(
            BlogDbContext context,
            IStaticPageCompiler pageCompiler,
            ILlmShareImporter llmImporter,
            ILogger<PostsController> logger)
        {
            _context = context;
            _pageCompiler = pageCompiler;
            _llmImporter = llmImporter;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all blog posts with summary metadata.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<BlogPostSummaryDto>>> GetAllPosts()
        {
            var posts = await _context.BlogPosts
                .Include(p => p.Sections)
                .Include(p => p.Reactions)
                .OrderByDescending(p => p.UpdatedTime)
                .Select(p => new BlogPostSummaryDto
                {
                    Id = p.Id,
                    Guid = p.Guid,
                    Title = p.Title,
                    Author = p.Author,
                    CreatedTime = p.CreatedTime,
                    UpdatedTime = p.UpdatedTime,
                    IsPublished = p.IsPublished,
                    RootPostId = p.RootPostId,
                    PreviousVersionId = p.PreviousVersionId,
                    NextVersionId = p.NextVersionId,
                    SectionCount = p.Sections.Count,
                    ReactionCount = p.Reactions.Count,
                    StaticUrl = p.IsPublished ? $"/posts/{p.Id}.html" : null
                })
                .ToListAsync();

            return Ok(posts);
        }

        /// <summary>
        /// Retrieves a single blog post by ID with sections ordered by SortOrder and reactions.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<BlogPost>> GetPostById(int id)
        {
            var post = await _context.BlogPosts
                .Include(p => p.Sections.OrderBy(s => s.SortOrder))
                .Include(p => p.Reactions.OrderByDescending(r => r.CreatedAt))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound(new { message = $"Post with ID {id} not found." });
            }

            return Ok(post);
        }

        /// <summary>
        /// Retrieves all related versions and branches for a given post.
        /// </summary>
        [HttpGet("{id:int}/versions")]
        public async Task<ActionResult<List<VersionBranchDto>>> GetVersions(int id)
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var rootId = post.RootPostId > 0 ? post.RootPostId : post.Id;

            var relatedVersions = await _context.BlogPosts
                .Where(p => p.RootPostId == rootId || p.Id == rootId)
                .OrderBy(p => p.CreatedTime)
                .Select(p => new VersionBranchDto
                {
                    Id = p.Id,
                    Title = p.Title,
                    UpdatedTime = p.UpdatedTime,
                    PreviousVersionId = p.PreviousVersionId,
                    NextVersionId = p.NextVersionId,
                    IsCurrent = p.Id == id,
                    IsPublished = p.IsPublished
                })
                .ToListAsync();

            return Ok(relatedVersions);
        }

        /// <summary>
        /// Creates a new blog post draft.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<BlogPost>> CreatePost([FromBody] CreateBlogPostDto dto)
        {
            var now = DateTime.UtcNow;
            var post = new BlogPost
            {
                Title = string.IsNullOrWhiteSpace(dto.Title) ? "Untitled Stream of Consciousness" : dto.Title,
                Author = string.IsNullOrWhiteSpace(dto.Author) ? "Anonymous Learner" : dto.Author,
                CreatedTime = now,
                UpdatedTime = now,
                IsPublished = dto.IsPublished,
                Sections = dto.Sections.Select((s, index) => new PostSection
                {
                    SortOrder = s.SortOrder > 0 ? s.SortOrder : index + 1,
                    Type = s.Type,
                    Content = s.Content ?? string.Empty,
                    Prompt = s.Prompt,
                    LlmOutput = s.LlmOutput,
                    Provider = s.Provider,
                    ShareUrl = s.ShareUrl
                }).ToList()
            };

            _context.BlogPosts.Add(post);
            await _context.SaveChangesAsync();

            // Set root post ID to itself
            post.RootPostId = post.Id;
            await _context.SaveChangesAsync();

            if (post.IsPublished)
            {
                await _pageCompiler.CompileAndSaveAsync(post);
            }

            return CreatedAtAction(nameof(GetPostById), new { id = post.Id }, post);
        }

        /// <summary>
        /// Updates post sections or forks a new branch version if CreateBranch is requested.
        /// </summary>
        [HttpPut("{id:int}")]
        public async Task<ActionResult<BlogPost>> UpdatePost(int id, [FromBody] UpdateBlogPostDto dto)
        {
            var post = await _context.BlogPosts
                .Include(p => p.Sections)
                .Include(p => p.Reactions)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound(new { message = $"Post with ID {id} not found." });
            }

            var now = DateTime.UtcNow;

            // If branching is requested, fork this post into a new version entity
            if (dto.CreateBranch)
            {
                var branchedPost = new BlogPost
                {
                    Title = dto.Title + " (Fork)",
                    Author = dto.Author,
                    CreatedTime = now,
                    UpdatedTime = now,
                    IsPublished = dto.IsPublished,
                    PreviousVersionId = post.Id,
                    RootPostId = post.RootPostId > 0 ? post.RootPostId : post.Id,
                    Sections = dto.Sections.Select((s, index) => new PostSection
                    {
                        SortOrder = s.SortOrder > 0 ? s.SortOrder : index + 1,
                        Type = s.Type,
                        Content = s.Content ?? string.Empty,
                        Prompt = s.Prompt,
                        LlmOutput = s.LlmOutput,
                        Provider = s.Provider,
                        ShareUrl = s.ShareUrl
                    }).ToList()
                };

                _context.BlogPosts.Add(branchedPost);
                await _context.SaveChangesAsync();

                // Link next version
                post.NextVersionId = branchedPost.Id;
                await _context.SaveChangesAsync();

                if (branchedPost.IsPublished)
                {
                    await _pageCompiler.CompileAndSaveAsync(branchedPost);
                }

                return CreatedAtAction(nameof(GetPostById), new { id = branchedPost.Id }, branchedPost);
            }

            // Normal in-place update
            post.Title = dto.Title;
            post.Author = dto.Author;
            post.UpdatedTime = now;
            post.IsPublished = dto.IsPublished;

            // Remove previous sections and replace with updated set
            _context.PostSections.RemoveRange(post.Sections);
            post.Sections = dto.Sections.Select((s, index) => new PostSection
            {
                BlogPostId = post.Id,
                SortOrder = s.SortOrder > 0 ? s.SortOrder : index + 1,
                Type = s.Type,
                Content = s.Content ?? string.Empty,
                Prompt = s.Prompt,
                LlmOutput = s.LlmOutput,
                Provider = s.Provider,
                ShareUrl = s.ShareUrl
            }).ToList();

            await _context.SaveChangesAsync();

            if (post.IsPublished)
            {
                await _pageCompiler.CompileAndSaveAsync(post);
            }

            return Ok(post);
        }

        /// <summary>
        /// Restores a post to the content of a previous version.
        /// </summary>
        [HttpPost("{id:int}/restore/{versionId:int}")]
        public async Task<ActionResult<BlogPost>> RestoreVersion(int id, int versionId)
        {
            var targetPost = await _context.BlogPosts
                .Include(p => p.Sections)
                .Include(p => p.Reactions)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (targetPost == null)
            {
                return NotFound(new { message = $"Post with ID {id} not found." });
            }

            var sourceVersion = await _context.BlogPosts
                .Include(p => p.Sections)
                .FirstOrDefaultAsync(p => p.Id == versionId);

            if (sourceVersion == null)
            {
                return NotFound(new { message = $"Version with ID {versionId} not found." });
            }

            targetPost.Title = sourceVersion.Title;
            targetPost.Author = sourceVersion.Author;
            targetPost.UpdatedTime = DateTime.UtcNow;

            _context.PostSections.RemoveRange(targetPost.Sections);
            targetPost.Sections = sourceVersion.Sections.Select(s => new PostSection
            {
                BlogPostId = targetPost.Id,
                SortOrder = s.SortOrder,
                Type = s.Type,
                Content = s.Content,
                Prompt = s.Prompt,
                LlmOutput = s.LlmOutput,
                Provider = s.Provider,
                ShareUrl = s.ShareUrl
            }).ToList();

            await _context.SaveChangesAsync();

            if (targetPost.IsPublished)
            {
                await _pageCompiler.CompileAndSaveAsync(targetPost);
            }

            return Ok(targetPost);
        }

        /// <summary>
        /// Manually triggers static HTML compilation into /var/www/static_blogs/{id}.html.
        /// </summary>
        [HttpPost("{id:int}/compile")]
        public async Task<IActionResult> CompileStaticHtml(int id)
        {
            var post = await _context.BlogPosts
                .Include(p => p.Sections)
                .Include(p => p.Reactions)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound(new { message = $"Post with ID {id} not found." });
            }

            var savedPath = await _pageCompiler.CompileAndSaveAsync(post);
            return Ok(new
            {
                success = true,
                message = "Static page compiled successfully.",
                filePath = savedPath,
                staticUrl = $"/posts/{post.Id}.html"
            });
        }

        /// <summary>
        /// Adds a reader highlighted commentary reaction with Chess.com classification.
        /// </summary>
        [HttpPost("{id:int}/reactions")]
        public async Task<ActionResult<ReaderReaction>> AddReaction(int id, [FromBody] CreateReaderReactionDto dto)
        {
            var post = await _context.BlogPosts
                .Include(p => p.Sections)
                .Include(p => p.Reactions)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound(new { message = $"Post with ID {id} not found." });
            }

            var reaction = new ReaderReaction
            {
                BlogPostId = id,
                PostSectionId = dto.PostSectionId,
                HighlightedText = dto.HighlightedText,
                StartOffset = dto.StartOffset,
                EndOffset = dto.EndOffset,
                Classification = dto.Classification,
                Comment = dto.Comment,
                ReaderName = string.IsNullOrWhiteSpace(dto.ReaderName) ? "Anonymous Reader" : dto.ReaderName,
                CreatedAt = DateTime.UtcNow
            };

            _context.ReaderReactions.Add(reaction);
            await _context.SaveChangesAsync();

            // Auto-recompile static HTML so static visitors see new annotations immediately
            try
            {
                await _pageCompiler.CompileAndSaveAsync(post);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not re-compile static HTML after reaction for Post {Id}", id);
            }

            return Ok(reaction);
        }

        /// <summary>
        /// Imports and extracts LLM prompt/response pairs from a shared chat URL.
        /// </summary>
        [HttpPost("import-llm-share")]
        public async Task<ActionResult<ImportLlmShareResponse>> ImportLlmShare([FromBody] ImportLlmShareRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return BadRequest(new { message = "URL cannot be empty." });
            }

            var result = await _llmImporter.ImportAsync(request.Url);
            return Ok(result);
        }
    }
}
