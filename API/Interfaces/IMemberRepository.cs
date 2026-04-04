using API.DTOs;
using API.Entities;
using API.Helpers;

namespace API.Interfaces;

public interface IMemberRepository
{
    Task<Member?> GetMemberByIdAsync(string id, CancellationToken ct = default);
    Task<PagedList<MemberDto>> GetMembersAsync(PaginationParams paginationParams, CancellationToken ct = default);
    Task<IReadOnlyList<Photo>> GetPhotosForMemberAsync(string memberId, CancellationToken ct = default);
    Task<bool> SaveAllAsync(CancellationToken ct = default);
    void Update(Member member);
}