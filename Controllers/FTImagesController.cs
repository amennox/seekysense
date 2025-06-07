using Microsoft.AspNetCore.Mvc;
using McpServer.Models;
using McpServer.Configuration;
using Microsoft.Extensions.Options;

[ApiController]
[Route("[controller]")]
public class FtImagesController : ControllerBase
{
    private readonly ElasticsearchService _esService;
    private readonly IWebHostEnvironment _env;
    private readonly FtImagesSettings _ftimagesSettings;

    // Constant for upload folder inside wwwroot
    private const string UploadFolderName = "ftimages";

    public FtImagesController(
        ElasticsearchService esService,
        IWebHostEnvironment env,
        IOptions<FtImagesSettings> ftimagesSettings)
    {
        _esService = esService;
        _env = env;
        _ftimagesSettings = ftimagesSettings.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromForm] FtImageUploadRequest request)
    {
        var file = request.File;
        if (file == null || file.Length == 0)
            return BadRequest("File is required.");

        var extension = Path.GetExtension(file.FileName);
        var newFileName = $"{Guid.NewGuid()}{extension}";
        var uploadsPath = Path.Combine(_env.WebRootPath, UploadFolderName);

        if (!Directory.Exists(uploadsPath))
            Directory.CreateDirectory(uploadsPath);

        var filePath = Path.Combine(uploadsPath, newFileName);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var ftImage = new FtImage
        {
            BusinessId = request.BusinessId,
            Scope = request.Scope,
            Image = newFileName,
            Description = request.Description
        };

        var result = await _esService.InsertFtImageAsync(ftImage);

        if (!result.IsValid)
            return StatusCode(500, "Failed to index the document.");

        return Ok(new { success = true, id = ftImage.Id });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        var ftImage = await _esService.GetFtImageAsync(id);
        if (ftImage == null)
            return NotFound("Image not found.");

        // Return also the complete image URL
        var imageUrl = _ftimagesSettings.BaseUrl.TrimEnd('/') + "/" + ftImage.Image;
        return Ok(new
        {
            ftImage.Id,
            ftImage.BusinessId,
            ftImage.Scope,
            ftImage.Description,
            ftImage.Image,
            ImageUrl = imageUrl
        });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var ftImage = await _esService.GetFtImageAsync(id);
        if (ftImage == null)
            return NotFound("Image not found.");

        var uploadsPath = Path.Combine(_env.WebRootPath, UploadFolderName);
        var filePath = Path.Combine(uploadsPath, ftImage.Image);

        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);

        var success = await _esService.DeleteFtImageAsync(id);

        if (!success)
            return StatusCode(500, "Failed to delete the document from the index.");

        return Ok(new { success = true });
    }

    // GET /ftimages?scope=myScope&page=1&pageSize=50
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? scope,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 50) pageSize = 50;

        var (items, total) = await _esService.GetFtImagesByScopeAsync(scope, page, pageSize);

        var result = items.Select(img => new
        {
            img.Id,
            img.BusinessId,
            img.Scope,
            img.Description,
            img.Image,
            ImageUrl = _ftimagesSettings.BaseUrl.TrimEnd('/') + "/" + img.Image
        });

        return Ok(new
        {
            items = result,
            page,
            pageSize,
            total
        });
    }
}
