using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class MemberRepository(AppDbContext context) : IMemberRepository
{
    public Task<Member?> GetMemberByIdAsync(string id, CancellationToken ct = default)
    {
        return context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public Task<Member?> GetMemberForUpdateAsync(string id, CancellationToken ct = default)
    {
        return context.Members
            .Include(x => x.User)
            .SingleOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<PagedList<MemberDto>> GetMembersAsync(
        PaginationParams paginationParams, CancellationToken ct = default)
    {
        var query = context.Members
            .AsNoTracking()
            .Select(m => new MemberDto
            {
                Id = m.Id,
                DisplayName = m.DisplayName,
                ImageUrl = m.ImageUrl,
                City = m.City,
                Country = m.Country,
                Gender = m.Gender,
                DateOfBirth = m.DateOfBirth,
            });

        return await PagedList<MemberDto>.CreateAsync(
            query,
            paginationParams.PageNumber,
            paginationParams.PageSize,
            ct
        );
    }

    public async Task<IReadOnlyList<Photo>> GetPhotosForMemberAsync(
        string memberId, CancellationToken ct = default)
    {
        return await context.Members
            .Where(x => x.Id == memberId)
            .SelectMany(x => x.Photos)
            .AsNoTracking()
            .ToListAsync(ct);
    }

    public async Task<bool> SaveAllAsync(CancellationToken ct = default)
    {
        return await context.SaveChangesAsync(ct) > 0;
    }

    public void Update(Member member)
    {
        context.Members.Update(member);
    }
}