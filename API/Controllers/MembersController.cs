using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;


public class MembersController(AppDbContext context) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppUser>>> GetMembers()
    {
        var members = await context
            .Users.AsNoTracking().ToListAsync();

        return members;
    }

    [Authorize]
    [HttpGet("{id}")] //api/members/xxx-xxx
    public async Task<ActionResult<AppUser>> GetMember(string id)
    {
        var member = await context
            .Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

        if (member == null) return NotFound();

        return member;
    }
}
