using System.Text.Json.Serialization;

namespace BlogEngine.Core.Entities
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
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

    [JsonConverter(typeof(JsonStringEnumConverter))]
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

        [JsonIgnore]
        public BlogPost? BlogPost { get; set; }
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

        [JsonIgnore]
        public BlogPost? BlogPost { get; set; }
    }
}
