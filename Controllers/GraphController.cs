using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class GraphController : ControllerBase
{
    private readonly Neo4jGraphService _graphService;

    public GraphController(Neo4jGraphService graphService)
    {
        _graphService = graphService;
    }

    // POST /graph/node
    [HttpPost("node")]
    public async Task<IActionResult> GetGraphFromNode([FromBody] GraphRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StartLabel) ||
            string.IsNullOrWhiteSpace(request.PropertyName) ||
            string.IsNullOrWhiteSpace(request.PropertyValue))
        {
            return BadRequest("Missing required parameters");
        }

        var (nodes, edges) = await _graphService.GetGraphFromNode(
            request.StartLabel, request.PropertyName, request.PropertyValue, request.Reverse);
      


        return Ok(new { nodes, edges });
    }

    [HttpGet("search-node")]
    public async Task<IActionResult> SearchNodes(
        [FromQuery] string label,
        [FromQuery] string property,
        [FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(label) ||
            string.IsNullOrWhiteSpace(property) ||
            string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Ok(new { items = new List<object>() });

        var results = await _graphService.SearchNodesByLabelAndPropertyAsync(label, property, q, 12);
        return Ok(new
        {
            items = results.Select(r => new
            {
                text = r.Text,
                nodeId = r.NodeId,
                label = r.Label,
                properties = r.Properties
            })
        });
    }
    // DTO per il body
    public class GraphRequest
    {
        public string StartLabel { get; set; } = "";
        public string PropertyName { get; set; } = "";
        public string PropertyValue { get; set; } = "";
        public bool Reverse { get; set; } = false; // nuovo campo
    }
}
