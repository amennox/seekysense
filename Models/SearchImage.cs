namespace McpServer.Configuration
{

    public class SearchImageRequest
    {
        public string? Text { get; set; } // Testo opzionale per la query multimodale
        public string? Scope { get; set; }
        public string BusinessId { get; set; } = null!;
        public string UserId { get; set; } = null!;
    }


    public class Image
    {
        public string Id { get; set; } = null!;
        public string Scope { get; set; } = null!;
        public string BusinessId { get; set; } = null!;
        public string? Title { get; set; }
        public string? Fulltext { get; set; }
        public string? ImageUrl { get; set; }
        public double[]? ImageVect { get; set; }
        public string? ElementId { get; set; }
        // altre proprietà che ti servono...
    }

    public class CreateImageFormDto
    {
        public string Scope { get; set; }
        public string BusinessId { get; set; }
        public string? Title { get; set; }
        public string? Fulltext { get; set; }
        public string? ElementId { get; set; }
        public IFormFile Image { get; set; }
    }

}