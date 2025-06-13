using Microsoft.AspNetCore.Mvc;
using McpServer.Configuration;
using McpServer.Models;

namespace McpServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [DisableRequestSizeLimit]
    public class ImagesController : ControllerBase
    {
        private readonly ImageService _imageService;
        private readonly EmbeddingService _emService;

        public ImagesController(ImageService imageService, EmbeddingService embService)
        {
            _imageService = imageService;
            _emService = embService;
        }

        // CREATE
        [HttpPost]
        public async Task<IActionResult> CreateImage([FromForm] CreateImageFormDto dto)
        {
            if (dto.Image == null || dto.Image.Length == 0)
                return BadRequest("Image file is required.");
            if (string.IsNullOrWhiteSpace(dto.BusinessId))
                return BadRequest("BusinessId is required");
            if (string.IsNullOrWhiteSpace(dto.Scope))
                return BadRequest("Scope is required");

            // 1. Salva la foto (es. wwwroot/uploads)
            var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsDir);
            var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(dto.Image.FileName);
            var filePath = Path.Combine(uploadsDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Image.CopyToAsync(stream);
            }

            // 2. Calcola o prendi l'embedding
            double[]? imageVect = null;
            if (!string.IsNullOrWhiteSpace(dto.ImageVect))
            {
                try
                {
                    imageVect = System.Text.Json.JsonSerializer.Deserialize<double[]>(dto.ImageVect);
                }
                catch (Exception ex)
                {
                    return BadRequest("ImageVect non valido: " + ex.Message);
                }
            }

            if (imageVect == null || imageVect.Length == 0)
            {
                byte[] imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                try
                {
                    imageVect = await _emService.GetEmbeddingFromImage(imageBytes,dto.Scope);
                    if (imageVect == null)
                        return StatusCode(500, "Impossibile calcolare embedding immagine");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Errore calcolo embedding: {ex.Message}");
                }
            }

            // 3. Costruisci l'oggetto Image
            var imgObj = new Image
            {
                Id = Guid.NewGuid().ToString("N"),
                Scope = dto.Scope,
                BusinessId = dto.BusinessId,
                Title = dto.Title,
                Fulltext = dto.Fulltext,
                ElementId = dto.ElementId,
                ImageUrl = $"/uploads/{fileName}",
                ImageVect = imageVect
            };

            var result = await _imageService.InsertImageAsync(imgObj);
            if (!result.IsValid)
                return StatusCode(500, "Failed to index image");

            return Ok(new { success = true, id = imgObj.Id, url = imgObj.ImageUrl });
        }


        // READALL (con filtro opzionale)
        [HttpGet]
        public async Task<IActionResult> GetImages([FromQuery] string? scope, [FromQuery] string? businessId)
        {
            var images = await _imageService.SearchImagesAsync(scope, businessId);
            return Ok(images);
        }

        // READONE
        [HttpGet("{id}")]
        public async Task<IActionResult> GetImage(string id)
        {
            var image = await _imageService.GetImageByIdAsync(id);
            if (image == null)
                return NotFound();
            return Ok(image);
        }

        // UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateImage(string id, [FromBody] Image updatedImage)
        {
            if (id != updatedImage.Id)
                return BadRequest("Id mismatch");

            var existing = await _imageService.GetImageByIdAsync(id);
            if (existing == null)
                return NotFound();

            if (updatedImage.ImageVect == null || updatedImage.ImageVect.Length == 0)
            {
                try
                {
                    using var http = new HttpClient();
                    var bytes = await http.GetByteArrayAsync(updatedImage.ImageUrl!);
                    var embedding = await _emService.GetEmbeddingFromImage(bytes);
                    if (embedding == null)
                        return StatusCode(500, "Impossibile calcolare l'embedding dell'immagine");
                    updatedImage.ImageVect = embedding;
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Errore nel calcolo embedding immagine: {ex.Message}");
                }
            }

            var result = await _imageService.UpdateImageAsync(updatedImage);
            if (!result.IsValid)
                return StatusCode(500, "Failed to update image");

            return Ok(new { success = true });
        }


        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteImage(string id)
        {
            var success = await _imageService.DeleteImageAsync(id);
            if (!success)
                return NotFound();

            return Ok(new { success = true });
        }
    }
}
