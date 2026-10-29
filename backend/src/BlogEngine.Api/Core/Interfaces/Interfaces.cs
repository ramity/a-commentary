using BlogEngine.Core.DTOs;
using BlogEngine.Core.Entities;

namespace BlogEngine.Core.Interfaces
{
    public interface IStaticPageCompiler
    {
        /// <summary>
        /// Compiles a BlogPost into a standalone static HTML file in the static directory (e.g., /var/www/static_blogs/{id}.html)
        /// and returns the absolute path of the generated file.
        /// </summary>
        Task<string> CompileAndSaveAsync(BlogPost post);

        /// <summary>
        /// Renders the static HTML string in-memory.
        /// </summary>
        string RenderToHtml(BlogPost post);
    }

    public interface ILlmShareImporter
    {
        /// <summary>
        /// Extracts prompt/response pairs from a shared chat link (ChatGPT, Claude, Gemini) or scrapes fallback metadata.
        /// </summary>
        Task<ImportLlmShareResponse> ImportAsync(string url);
    }
}
