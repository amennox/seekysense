using Microsoft.AspNetCore.Mvc;
using McpServer.Models;

namespace McpServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConfigurationController : ControllerBase
    {
        private readonly ConfigurationService _configurationService;

        public ConfigurationController(ConfigurationService configurationService)
        {
            _configurationService = configurationService;
        }

        // SCOPES

        [HttpPost("scopes")]
        public async Task<IActionResult> CreateScope([FromBody] Scope scope)
        {
            try
            {
                var result = await _configurationService.InsertScopeAsync(scope);
                return Ok(new { success = result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating Scope: {ex.Message}");
            }
        }

        [HttpGet("scopes")]
        public async Task<IActionResult> GetScopes()
        {
            var scopes = await _configurationService.GetAllScopesAsync();
            return Ok(scopes);
        }

        [HttpGet("scopes/{id}")]
        public async Task<IActionResult> GetScope(string id)
        {
            var scope = await _configurationService.GetScopeByIdAsync(id);
            if (scope == null)
                return NotFound();
            return Ok(scope);
        }

        [HttpPut("scopes/{id}")]
        public async Task<IActionResult> UpdateScope(string id, [FromBody] Scope updatedScope)
        {
            if (id != updatedScope.ScopeId)
                return BadRequest("ScopeId mismatch");

            var success = await _configurationService.UpdateScopeAsync(updatedScope);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("scopes/{id}")]
        public async Task<IActionResult> DeleteScope(string id)
        {
            var success = await _configurationService.DeleteScopeAsync(id);
            return success ? Ok(new { success = true }) : NotFound();
        }

        // BUSINESSAUTHS

        [HttpPost("businessauths")]
        public async Task<IActionResult> CreateBusinessAuth([FromBody] BusinessAuth auth)
        {
            await _configurationService.InsertBusinessAuthAsync(auth);
            return Ok(new { success = true });
        }

        [HttpGet("businessauths")]
        public async Task<IActionResult> GetBusinessAuths()
        {
            var auths = await _configurationService.GetAllBusinessAuthsAsync();
            return Ok(auths);
        }

        [HttpGet("businessauths/{id}")]
        public async Task<IActionResult> GetBusinessAuth(Guid id)
        {
            var auth = await _configurationService.GetBusinessAuthByIdAsync(id);
            if (auth == null)
                return NotFound();
            return Ok(auth);
        }

        [HttpPut("businessauths/{id}")]
        public async Task<IActionResult> UpdateBusinessAuth(Guid id, [FromBody] BusinessAuth updatedAuth)
        {
            if (id != updatedAuth.Id)
                return BadRequest("Id mismatch");

            var success = await _configurationService.UpdateBusinessAuthAsync(updatedAuth);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("businessauths/{id}")]
        public async Task<IActionResult> DeleteBusinessAuth(Guid id)
        {
            var success = await _configurationService.DeleteBusinessAuthAsync(id);
            return success ? Ok(new { success = true }) : NotFound();
        }

        // USERAUTHS

        [HttpPost("userauths")]
        public async Task<IActionResult> CreateUserAuth([FromBody] UserAuth auth)
        {
            await _configurationService.InsertUserAuthAsync(auth);
            return Ok(new { success = true });
        }

        [HttpGet("userauths")]
        public async Task<IActionResult> GetUserAuths()
        {
            var auths = await _configurationService.GetAllUserAuthsAsync();
            return Ok(auths);
        }

        [HttpGet("userauths/{id}")]
        public async Task<IActionResult> GetUserAuth(Guid id)
        {
            var auth = await _configurationService.GetUserAuthByIdAsync(id);
            if (auth == null)
                return NotFound();
            return Ok(auth);
        }

        [HttpPut("userauths/{id}")]
        public async Task<IActionResult> UpdateUserAuth(Guid id, [FromBody] UserAuth updatedAuth)
        {
            if (id != updatedAuth.Id)
                return BadRequest("Id mismatch");

            var success = await _configurationService.UpdateUserAuthAsync(updatedAuth);
            return success ? Ok(new { success = true }) : NotFound();
        }

        [HttpDelete("userauths/{id}")]
        public async Task<IActionResult> DeleteUserAuth(Guid id)
        {
            var success = await _configurationService.DeleteUserAuthAsync(id);
            return success ? Ok(new { success = true }) : NotFound();
        }




    }
}
