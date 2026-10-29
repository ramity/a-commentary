using BlogEngine.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlogEngine.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(BlogDbContext context, ILogger logger)
        {
            try
            {
                logger.LogInformation("Checking database connection and applying migrations/schema creation...");
                await context.Database.EnsureCreatedAsync();

                if (!await context.BlogPosts.AnyAsync())
                {
                    logger.LogInformation("Seeding initial AI stream-of-consciousness post...");

                    var seedPost = new BlogPost
                    {
                        Title = "Stream of Consciousness: Debugging Distributed Locks with Claude 3.5 Sonnet",
                        Author = "Alex Mercer",
                        CreatedTime = DateTime.UtcNow.AddHours(-12),
                        UpdatedTime = DateTime.UtcNow.AddMinutes(-30),
                        IsPublished = true,
                        RootPostId = 1,
                        Sections = new List<PostSection>
                        {
                            new PostSection
                            {
                                SortOrder = 1,
                                Type = SectionType.MarkdownText,
                                Content = "# The 2 AM Epiphany on Distributed Deadlocks\n\nI was staring at Redis lock contention graphs. Our payment settlement worker would deadlock every 400th batch during peak load. My mind was foggy, so I decided to pair-program with Claude to unpack my stream-of-consciousness theories on distributed consensus."
                            },
                            new PostSection
                            {
                                SortOrder = 2,
                                Type = SectionType.LLMConversation,
                                Provider = "Claude 3.5 Sonnet",
                                ShareUrl = "https://claude.ai/share/sample-deadlock-debugging",
                                Prompt = "Hey Claude, I have a Redlock implementation in C# with a 5000ms TTL. When worker threads perform bulk balance settlements, we get intermittent lock expiration before the MySQL transaction commits. Why would this happen under high load?",
                                LlmOutput = "The classic vulnerability with Redis-based distributed locks is the **Time-to-Live vs. GC Pause / IO Stall** divergence.\n\n1. **Long GC Pauses or CPU throttling**: If your .NET runtime hits a Gen 2 garbage collection pause or hypervisor CPU starvation, the local process freezes while the Redis TTL continues ticking.\n2. **Long DB Transaction Locks**: If MySQL row locks stall waiting on secondary index locks, the lock expires in Redis. Another worker acquires the lock, leading to split-brain double settlement.\n\n**Recommendation**: Implement a lock renewal background heartbeat (`CancellationTokenSource` timer) and enforce fencing tokens (monotonically increasing version numbers) checked inside your MySQL transaction."
                            },
                            new PostSection
                            {
                                SortOrder = 3,
                                Type = SectionType.Quote,
                                Content = "Distributed systems don't fail when everything breaks; they fail when one node thinks it's faster than physics."
                            },
                            new PostSection
                            {
                                SortOrder = 4,
                                Type = SectionType.CodeSnippet,
                                Content = @"// Fencing token verification in settlement transaction
public async Task<bool> SettleBatchAsync(Guid batchId, long fencingToken)
{
    using var tx = await _db.BeginTransactionAsync();
    var currentBatch = await _db.Batches.FindAsync(batchId);
    
    // Check monotonic order to prevent split-brain execution
    if (fencingToken <= currentBatch.LastProcessedFence)
    {
        _logger.LogWarning(""Stale fence token detected! Aborting to prevent race condition."");
        return false;
    }
    
    currentBatch.LastProcessedFence = fencingToken;
    await _db.SaveChangesAsync();
    await tx.CommitAsync();
    return true;
}"
                            },
                            new PostSection
                            {
                                SortOrder = 5,
                                Type = SectionType.MarkdownText,
                                Content = "### Conclusion & Retrospective\n\nThe AI's recommendation to introduce a heartbeat renewal loop together with monotonic fencing tokens completely eliminated our 2 AM alerts. The key takeaway: never let a distributed lock dictate truth without fencing token verification in your database."
                            }
                        }
                    };

                    context.BlogPosts.Add(seedPost);
                    await context.SaveChangesAsync();

                    // Seed sample reader reactions with Chess.com commentary moves
                    var section1 = seedPost.Sections.First(s => s.SortOrder == 1);
                    var section2 = seedPost.Sections.First(s => s.SortOrder == 2);
                    var section4 = seedPost.Sections.First(s => s.SortOrder == 4);

                    var reactions = new List<ReaderReaction>
                    {
                        new ReaderReaction
                        {
                            BlogPostId = seedPost.Id,
                            PostSectionId = section1.Id,
                            HighlightedText = "payment settlement worker would deadlock every 400th batch",
                            StartOffset = 64,
                            EndOffset = 124,
                            Classification = MoveClassification.Inaccuracy,
                            Comment = "A timeout or lock expiration isn't technically a deadlock; it's a livelock or split-brain contention.",
                            ReaderName = "DrDistributed",
                            CreatedAt = DateTime.UtcNow.AddHours(-10)
                        },
                        new ReaderReaction
                        {
                            BlogPostId = seedPost.Id,
                            PostSectionId = section2.Id,
                            HighlightedText = "enforce fencing tokens (monotonically increasing version numbers)",
                            StartOffset = 478,
                            EndOffset = 544,
                            Classification = MoveClassification.Brilliant,
                            Comment = "Martin Kleppmann's famous critique of Redlock in action! Fencing tokens are the ONLY safe way to handle distributed lock expirations.",
                            ReaderName = "SystemsArchitect",
                            CreatedAt = DateTime.UtcNow.AddHours(-8)
                        },
                        new ReaderReaction
                        {
                            BlogPostId = seedPost.Id,
                            PostSectionId = section4.Id,
                            HighlightedText = "fencingToken <= currentBatch.LastProcessedFence",
                            StartOffset = 265,
                            EndOffset = 312,
                            Classification = MoveClassification.Best,
                            Comment = "Super clean defensive guard clause. Simple and bulletproof.",
                            ReaderName = "GMMoves",
                            CreatedAt = DateTime.UtcNow.AddHours(-5)
                        }
                    };

                    context.ReaderReactions.AddRange(reactions);
                    await context.SaveChangesAsync();

                    logger.LogInformation("Database seeded successfully with Post ID {Id}", seedPost.Id);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while seeding the database.");
                throw;
            }
        }
    }
}
