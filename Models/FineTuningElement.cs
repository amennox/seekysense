namespace McpServer.Models
{
    public class FineTuningElement
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Question { get; set; } = null!;
        public string Answer { get; set; } = null!;
        public string Scope { get; set; } = null!;
        public bool IsPositive { get; set; }
        public DateTime DateTime { get; set; }
        public string Reference { get; set; } = null!;
        public string Source { get; set; } = null!;
        public string BusinessId { get; set; } = null!;
    }

    public class FtImage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string BusinessId { get; set; }
        public required string Scope { get; set; }
        public required string Image { get; set; } // Nome del file salvato
        public required string Description { get; set; }
    }

    public class FtImageUploadRequest
    {
        public IFormFile File { get; set; } = null!;
        public string BusinessId { get; set; } = null!;
        public string Scope { get; set; } = null!;
        public string Description { get; set; } = null!;
    }

}
