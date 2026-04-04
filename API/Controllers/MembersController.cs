using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class MembersController(IMemberRepository memberRepository) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MemberDto>>> GetMembers(
        [FromQuery] PaginationParams paginationParams,
        CancellationToken ct)
    {
        var members = await memberRepository.GetMembersAsync(paginationParams, ct);

        Response.AddPaginationHeader(members);

        return Ok(members);
    }

    [HttpGet("{id}")] //api/members/xxx-xxx
    public async Task<ActionResult<Member>> GetMember(string id, CancellationToken ct)
    {
        var member = await memberRepository.GetMemberByIdAsync(id, ct);

        if (member == null) return NotFound();

        return member;
    }

    [HttpGet("{id}/photos")]
    public async Task<ActionResult<IReadOnlyList<Photo>>> GetMemberPhotos(
        string id, CancellationToken ct)
    {
        return Ok(await memberRepository.GetPhotosForMemberAsync(id, ct));
    }
}