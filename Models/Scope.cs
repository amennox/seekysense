// Models/Scope.cs
namespace McpServer.Models
{
    public class Scope
    {
        public string ScopeId { get; set; } = null!;
        public string ScopeType { get; set; } = null!;
        public string ScopeDataLiveAuthType { get; set; } = null!;
        public string ScopeDataLiveAuthMethod { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string DescriptionFullText { get; set; } = null!;
    }
}

