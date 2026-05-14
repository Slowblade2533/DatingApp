using System.Security.Claims;
using API.DTOs;
using API.Entities;
using API.Extensions;
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
        var photos = await memberRepository.GetPhotosForMemberAsync(id, ct);

        return Ok(photos);
    }

    [HttpPut]
    public async Task<ActionResult> UpdateMember(MemberUpdateDto memberUpdateDto, CancellationToken ct)
    {
        var memberId = User.GetMemberId();

        var member = await memberRepository.GetMemberForUpdateAsync(memberId, ct);

        if (member == null)
            return BadRequest("Could not get member");

        member.DisplayName = memberUpdateDto.DisplayName ?? member.DisplayName;
        member.Description = memberUpdateDto.Description ?? member.Description;
        member.City = memberUpdateDto.City ?? member.City;
        member.Country = memberUpdateDto.Country ?? member.Country;

        member.User.DisplayName = memberUpdateDto.DisplayName ?? member.User.DisplayName;

        memberRepository.Update(member);

        if (await memberRepository.SaveAllAsync(ct))
            return NoContent();

        return BadRequest("Failed to update member");
    }

}