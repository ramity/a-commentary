using BlogEngine.Core.Entities;

namespace BlogEngine.Core.DTOs
{
    public class CreateBlogPostDto
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = false;
        public List<PostSectionDto> Sections { get; set; } = new();
    }

    public class UpdateBlogPostDto
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public bool IsPublished { get; set; } = false;
        public bool CreateBranch { get; set; } = false; // When true, forks a new branch version
        public List<PostSectionDto> Sections { get; set; } = new();
    }

    public class PostSectionDto
    {
        public int? Id { get; set; }
        public int SortOrder { get; set; }
        public SectionType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? Prompt { get; set; }
        public string? LlmOutput { get; set; }
        public string? Provider { get; set; }
        public string? ShareUrl { get; set; }
    }

    public class CreateReaderReactionDto
    {
        public int PostSectionId { get; set; }
        public string HighlightedText { get; set; } = string.Empty;
        public int StartOffset { get; set; }
        public int EndOffset { get; set; }
        public MoveClassification Classification { get; set; }
        public string Comment { get; set; } = string.Empty;
        public string ReaderName { get; set; } = "Anonymous";
    }

    public class ImportLlmShareRequest
    {
        public string Url { get; set; } = string.Empty;
    }

    public class ImportLlmShareResponse
    {
        public bool Success { get; set; }
        public string Provider { get; set; } = string.Empty;
        public string ShareUrl { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<LlmMessagePairDto> Messages { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class LlmMessagePairDto
    {
        public string Prompt { get; set; } = string.Empty;
        public string LlmOutput { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
    }

    public class BlogPostSummaryDto
    {
        public int Id { get; set; }
        public string Guid { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedTime { get; set; }
        public DateTime UpdatedTime { get; set; }
        public bool IsPublished { get; set; }
        public int RootPostId { get; set; }
        public int? PreviousVersionId { get; set; }
        public int? NextVersionId { get; set; }
        public int SectionCount { get; set; }
        public int ReactionCount { get; set; }
        public string? StaticUrl { get; set; }
    }

    public class VersionBranchDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime UpdatedTime { get; set; }
        public int? PreviousVersionId { get; set; }
        public int? NextVersionId { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsPublished { get; set; }
    }
}
