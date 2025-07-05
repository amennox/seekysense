using System.Text.Json.Serialization;

namespace McpServer.Models
{
    public class Element
    {
        public required string Id { get; set; }
        public required string Scope { get; set; }
        public required string Title { get; set; }
        public List<Command>? Commands { get; set; }
        public string? LiveDataUrl { get; set; }
        public string? LiveDataTemplate { get; set; }
        public string? LiveDataValidation { get; set; }
        public required string Fulltext { get; set; }
        public double[]? FulltextVect { get; set; }
        public double[]? FulltextVectFT { get; set; }
        public string? BusinessId { get; set; }
        public string? ExternalId { get; set; }
        public string? ChunkSection { get; set; }
    }

    public class Command
    {
        public string? CommandName { get; set; }
        public string? CommandUrl { get; set; }
    }

    public class ElementResponseDto
    {
        public string Id { get; set; } = default!;
        public string Scope { get; set; } = default!;
        public string BusinessId { get; set; } = default!;
        public string Title { get; set; } = default!;
        public IEnumerable<Command> Commands { get; set; } = Array.Empty<Command>();
        public string Fulltext { get; set; } = default!;
        public string? FulltextLive { get; set; }
        public int RelevanceScore { get; set; }
    }

    public class FileUploadRequest
    {
        public IFormFile File { get; set; }
        public string FolderType { get; set; } // "uploads" oppure "video"
    }

    public class AggregatedElementResult
    {
        public Element ParentElement { get; set; } = null!;
        public double Score { get; set; }
        public List<Element> Chunks { get; set; } = new();
    }

    public class AggregatedElementResultChunks
    {
        public string ExternalId { get; set; } = "";
        public double AvgScore { get; set; }
        public List<ArticleChunk> Chunks { get; set; } = new();


    }
    
     public class ArticleChunk
        {
            public string Id { get; set; } = "";
            public string Title { get; set; } = "";
            public string ChunkSection { get; set; } = "";
            public string Fulltext { get; set; } = "";
            public double Score { get; set; }
        }
}
