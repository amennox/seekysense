using Microsoft.AspNetCore.Mvc;
using McpServer.Configuration;
using McpServer.Models;

namespace McpServer.Controllers
{

    [ApiController]
    [Route("[controller]")]
    [DisableRequestSizeLimit] 
    public class MediaController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public MediaController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] FileUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("File non fornito.");

            // Scegli la cartella in base al parametro
            string targetFolder = request.FolderType?.ToLower() switch
            {
                "uploads" => "uploads",
                "videos" => "videos",
                _ => null
            };

            if (targetFolder == null)
                return BadRequest("FolderType non valido. Usa 'uploads' o 'video'.");

            // Path fisico
            var uploadsPath = Path.Combine(_env.WebRootPath, targetFolder);

            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.File.FileName)}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            // Se vuoi puoi restituire il path relativo o l’URL del file caricato
            var url = $"/{targetFolder}/{fileName}";
            return Ok(new { success = true, url });
        }
    }
}