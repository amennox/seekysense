using Microsoft.AspNetCore.Mvc;
using System.Text.Json;


[ApiController]
[Route("[controller]")]
public class RenderController : ControllerBase
{
    private readonly RenderTextService _renderText;

    public RenderController(RenderTextService renderText)
    {
        _renderText = renderText;
    }

    [HttpPost]
    public IActionResult Post([FromBody] RenderRequest request)
    {
        var rendered = _renderText.Render(request.Template, request.Data);
        return Ok(new { output = rendered });
    }

    public class RenderRequest
    {
        public required string Template { get; set; }
        public required JsonElement Data { get; set; }
    }
}
