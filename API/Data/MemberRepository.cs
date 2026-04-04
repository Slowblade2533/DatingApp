using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class MemberRepository(AppDbContext context) : IMemberRepository
{
    public async Task<Member?> GetMemberByIdAsync(string id, CancellationToken ct = default)
    {
        return await context.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }

    public async Task<PagedList<MemberDto>> GetMembersAsync(
        PaginationParams paginationParams, CancellationToken ct = default)
    {
        // PERF-01: Projection — ดึงเฉพาะ Column ที่จำเป็นสำหรับหน้า List
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
                DateofBirth = m.DateofBirth,
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