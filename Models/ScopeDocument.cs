namespace McpServer.Models
{
    public class ScopeDocument
    {
        public string ScopeId { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string DescriptionFullText { get; set; } = null!;
        public double[]? DescriptionVector { get; set; }
    }
}