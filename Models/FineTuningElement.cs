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
}
