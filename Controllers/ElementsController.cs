using Microsoft.AspNetCore.Mvc;
using McpServer.Models;
using McpServer.Configuration;

namespace McpServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ElementsController : ControllerBase
    {
        private readonly ElementService _elementService;

        public ElementsController(ElementService elementService)
        {
            _elementService = elementService;
        }

        // CREATE - Inserisce nuovo elemento
        [HttpPost]
        public async Task<IActionResult> CreateElement([FromBody] Element element)
        {
            if (string.IsNullOrWhiteSpace(element.Fulltext))
                return BadRequest("fulltext cannot be empty");

            var result = await _elementService.InsertElementAsync(element);
            if (!result.IsValid)
                return StatusCode(500, "Failed to index element");

            return Ok(new
            {
                success = true,
                id = result.Id,
                index = result.Index
            });
        }

        // READALL - Lista elementi con filtro opzionale
        [HttpGet]
        public async Task<IActionResult> GetElements([FromQuery] string? scope, [FromQuery] string? businessId)
        {
            var elements = await _elementService.SearchElementsAsync(scope, businessId);
            return Ok(elements);
        }

        // READONE - Leggi elemento per ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetElement(string id)
        {
            var element = await _elementService.GetElementByIdAsync(id);
            if (element == null)
                return NotFound();
            return Ok(element);
        }

        // UPDATE - Aggiorna elemento
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateElement(string id, [FromBody] Element updatedElement)
        {
            if (id != updatedElement.Id)
                return BadRequest("Id mismatch");

            var existing = await _elementService.GetElementByIdAsync(id);
            if (existing == null)
                return NotFound();

            var result = await _elementService.UpdateElementAsync(updatedElement);
            if (!result.IsValid)
                return StatusCode(500, "Failed to update element");

            return Ok(new { success = true });
        }

        // DELETE - Cancella elemento
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteElement(string id)
        {
            var success = await _elementService.DeleteElementAsync(id);
            if (!success)
                return NotFound();

            return Ok(new { success = true });
        }
    }
}
