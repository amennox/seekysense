namespace McpServer.Models
{
    public class UserAuth
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = null!;
        public string BusinessId { get; set; } = null!;
        public string ScopeId { get; set; } = null!;
        public string ApiKey { get; set; } = null!;
    }
}
