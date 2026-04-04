using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

/// <summary>
/// Test controller for error handling verification — only available in Development.
/// </summary>
#if DEBUG
public class BuggyController(IHostEnvironment env) : BaseApiController
{
    [HttpGet("auth")]
    public IActionResult GetAuth()
    {
        if (!env.IsDevelopment()) return NotFound();
        return Unauthorized();
    }

    [HttpGet("not-found")]
    public IActionResult GetNotFound()
    {
        if (!env.IsDevelopment()) return NotFound();
        return NotFound();
    }

    [HttpGet("server-error")]
    public IActionResult GetServerError()
    {
        if (!env.IsDevelopment()) return NotFound();
        throw new Exception("This is a server error");
    }

    [HttpGet("bad-request")]
    public IActionResult GetBadRequest()
    {
        if (!env.IsDevelopment()) return NotFound();
        return BadRequest("This was not a good request");
    }
}
#endif