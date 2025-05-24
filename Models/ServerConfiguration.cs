namespace McpServer.Configuration
{
    public class EmbeddingSettings
    {
        public string BaseUrl { get; set; } = "";
        public string Model { get; set; } = "";
    }

    public class EmbeddingFTSettings
    {
        public string BaseUrl { get; set; } = "";
        public string Model { get; set; } = "";
    }
    public class ElasticSettings
    {
        public string BaseUrl { get; set; } = "";
    }

    public class SummarizeSettings
    {
        public string BaseUrl { get; set; } = "";
        public string Model { get; set; } = "";
        public string PromptTemplate { get; set; } = "";
    }
}