using Microsoft.AspNetCore.Mvc;
using McpServer.Models;

namespace McpServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FineTuningElementController : ControllerBase
    {
        private readonly ElasticsearchService _esService;

        public FineTuningElementController(ElasticsearchService esService)
        {
            _esService = esService;
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> CreateFineTuningElement([FromBody] FineTuningElement element)
        {
            var result = await _esService.InsertFineTuningElementAsync(element);
            if (!result.IsValid)
                return StatusCode(500, "Errore durante l'inserimento dell'elemento");

            return Ok(new { success = true, id = result.Id });
        }

        // READ ALL
        [HttpGet]
        public async Task<IActionResult> GetFineTuningElements([FromQuery] string? scope, [FromQuery] string? businessId)
        {
            var elements = await _esService.SearchFineTuningElementsAsync(scope, businessId);
            return Ok(elements);
        }

        // READ BY ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFineTuningElement(string id)
        {
            var element = await _esService.GetFineTuningElementByIdAsync(id);
            if (element == null)
                return NotFound();

            return Ok(element);
        }

        // UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFineTuningElement(string id, [FromBody] FineTuningElement updatedElement)
        {
            if (id != updatedElement.Id)
                return BadRequest("Id mismatch");

            var result = await _esService.UpdateFineTuningElementAsync(id, updatedElement);
            if (!result.IsValid)
                return StatusCode(500, "Errore durante l'aggiornamento dell'elemento");

            return Ok(new { success = true });
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFineTuningElement(string id)
        {
            var result = await _esService.DeleteFineTuningElementAsync(id);
            if (!result.IsValid)
                return NotFound();

            return Ok(new { success = true });
        }

        // CREATE MULTIPLE
        [HttpPost("batch")]
        public async Task<IActionResult> CreateFineTuningElementsBatch([FromBody] List<FineTuningElement> elements)
        {
            if (elements == null || !elements.Any())
                return BadRequest("La lista degli elementi è vuota.");

            var result = await _esService.InsertFineTuningElementsAsync(elements);

            if (result.Errors)
            {
                var failedItems = result.ItemsWithErrors.Select(e => new
                {
                    e.Id,
                    e.Error.Reason
                });
                return StatusCode(500, new { success = false, errors = failedItems });
            }

            return Ok(new
            {
                success = true,
                inserted = result.Items.Count
            });
        }

    }
}
