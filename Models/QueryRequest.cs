public class QueryRequest
{
    public required string Query { get; set; }
    public string? Scope { get; set; }
    public required string BusinessId { get; set; }
    public required string UserId { get; set; }
    public string? Type { get; set; } // "standard" | "fine-tuned" | "mixed"
    public string? QueryNegative { get; set; } // "it" | "en"
    public bool? GroupByExternalId { get; set; }
}
