using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("[controller]")]
public class Status : ControllerBase
{
    [HttpGet]
    public IEnumerable<string> Get()
    {
        return new[] { "MCP Server Status", "online" };
    }
}
