using Microsoft.EntityFrameworkCore;

namespace API.Helpers;

public sealed class PagedList<T> : List<T>
{
    public int CurrentPage { get; }
    public int TotalPages { get; }
    public int PageSize { get; }
    public int TotalCount { get; }

    public PagedList(IEnumerable<T> items, int count, int pageNumber, int pageSize)
    {
        CurrentPage = Math.Max(1, pageNumber);
        PageSize = Math.Clamp(pageSize, 1, PaginationParams.MaxPageSize);
        TotalCount = Math.Max(0, count);
        TotalPages = TotalCount == 0
            ? 0
            : (int)Math.Ceiling(TotalCount / (double)PageSize);

        AddRange(items ?? Enumerable.Empty<T>());
    }

    // ฟังก์ชันนี้จะรับหน้าที่ไปสั่ง .Skip() และ .Take() ที่ Database ให้เราอัตโนมัติ
    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> source,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var safePageNumber = Math.Max(1, pageNumber);
        var safePageSize = Math.Clamp(pageSize, 1, PaginationParams.MaxPageSize);

        var count = await source.CountAsync(ct);

        if (count == 0)
            return new PagedList<T>([], count, safePageNumber, safePageSize);

        var skip = (long)(safePageNumber - 1) * safePageSize;

        if (skip >= count)
            return new PagedList<T>([], count, safePageNumber, safePageSize);

        var items = await source
            .Skip((int)skip)
            .Take(safePageSize)
            .ToListAsync(ct);

        return new PagedList<T>(items, count, safePageNumber, safePageSize);
    }
}
