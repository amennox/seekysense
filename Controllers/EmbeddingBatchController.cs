using Microsoft.AspNetCore.Mvc;
using McpServer.Models;
using System.Threading.Tasks;

namespace McpServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EmbeddingBatchController : ControllerBase
    {
        private readonly ElasticsearchService _esService;
        private readonly EmbeddingService _embeddingService;

        public EmbeddingBatchController(ElasticsearchService esService, EmbeddingService embeddingService)
        {
            _esService = esService;
            _embeddingService = embeddingService;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromQuery] string? type)
        {
            string embeddingType = string.IsNullOrWhiteSpace(type) ? "fine-tuned" : type.ToLower();
            if (embeddingType != "standard" && embeddingType != "fine-tuned")
                return BadRequest("Invalid type. Must be 'standard' or 'fine-tuned'.");

            var allElements = await _esService.GetAllElementsAsync();
            int updatedCount = 0;
            int errorCount = 0;

            foreach (var element in allElements)
            {
                if (string.IsNullOrWhiteSpace(element.Fulltext))
                    continue;

                var embedding = await _embeddingService.GetEmbedding(element.Fulltext, embeddingType);
                if (embedding == null)
                {
                    errorCount++;
                    continue;
                }

                if (embeddingType == "standard")
                    element.FulltextVect = embedding;
                else
                {
                    // Reflection per compatibilità se proprietà non mappata direttamente nel model
                    var prop = element.GetType().GetProperty("FulltextVectFT");
                    if (prop != null)
                        prop.SetValue(element, embedding);
                }

                var result = await _esService.InsertElementAsync(element);
                if (result.IsValid)
                    updatedCount++;
                else
                    errorCount++;
            }

            return Ok(new
            {
                success = true,
                type = embeddingType,
                updated = updatedCount,
                failed = errorCount
            });
        }
    }
}
