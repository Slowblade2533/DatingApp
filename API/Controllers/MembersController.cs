using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize]
public class MembersController(
    IMemberRepository memberRepository,
    IPhotoService photoService) : BaseApiController
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MemberDto>>> GetMembers(
        [FromQuery] PaginationParams paginationParams,
        CancellationToken ct = default)
    {
        var members = await memberRepository.GetMembersAsync(paginationParams, ct);

        Response.AddPaginationHeader(members);

        return Ok(members);
    }

    [HttpGet("{id}")] //api/members/xxx-xxx
    public async Task<ActionResult<Member>> GetMember(string id, CancellationToken ct = default)
    {
        var member = await memberRepository.GetMemberByIdAsync(id, ct);

        return member ?? (ActionResult<Member>)NotFound();
    }

    [HttpGet("{id}/photos")]
    public async Task<ActionResult<IReadOnlyList<Photo>>> GetMemberPhotos(
        string id, CancellationToken ct = default)
    {
        var photos = await memberRepository.GetPhotosForMemberAsync(id, ct);

        return Ok(photos);
    }

    [HttpPut]
    public async Task<ActionResult> UpdateMember(MemberUpdateDto memberUpdateDto, CancellationToken ct = default)
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

    [HttpPost("add-photo")]
    public async Task<ActionResult<Photo>> AddPhoto([FromForm] IFormFile file, CancellationToken ct = default)
    {
        var member = await memberRepository.GetMemberForUpdateAsync(User.GetMemberId(), ct);

        if (member == null)
            return BadRequest("Cannot update member");

        var result = await photoService.UploadPhotoAsync(file);

        if (result.Error != null)
            return BadRequest(result.Error.Message);

        var photo = new Photo
        {
            Url = result.SecureUrl.AbsoluteUri,
            PublicId = result.PublicId,
            MemberId = User.GetMemberId()
        };

        if (member.ImageUrl == null)
        {
            member.ImageUrl = photo.Url;
            member.User.ImageUrl = photo.Url;
        }

        member.Photos.Add(photo);

        if (await memberRepository.SaveAllAsync(ct))
            return photo;

        return BadRequest("Problem adding photo");
    }

    [HttpPut("set-main-photo/{photoId}")]
    public async Task<ActionResult> SetMainPhoto(int photoId, CancellationToken ct = default)
    {
        var member = await memberRepository.GetMemberForUpdateAsync(User.GetMemberId(), ct);

        if (member == null)
            return BadRequest("Cannot get member from token");

        var photo = member.Photos.SingleOrDefault(x => x.Id == photoId);

        if (member.ImageUrl == photo?.Url || photo == null)
            return BadRequest("Cannot set this as main image");

        member.ImageUrl = photo.Url;
        member.User.ImageUrl = photo.Url;

        if (await memberRepository.SaveAllAsync(ct))
            return NoContent();

        return BadRequest("Problem setting main photo");

    }

    [HttpDelete("delete-photo/{photoID}")]
    public async Task<ActionResult> DeletePhoto(int photoId, CancellationToken ct = default)
    {
        var member = await memberRepository.GetMemberForUpdateAsync(User.GetMemberId(), ct);

        if (member == null)
            return BadRequest("Cannot get member from token");

        var photo = member.Photos.SingleOrDefault(x => x.Id == photoId);

        if (photo == null || photo.Url == member.ImageUrl)
            return BadRequest("This photo cannot be deleted");

        if (photo.PublicId != null)
        {
            var result = await photoService.DeletePhotoAsync(photo.PublicId, ct);

            if (result.Error != null)
                return BadRequest(result.Error.Message);
        }

        member.Photos.Remove(photo);

        if (await memberRepository.SaveAllAsync(ct))
            return Ok();

        return BadRequest("Problem deleting the photo");
    }

}