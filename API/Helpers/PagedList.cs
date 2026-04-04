using Microsoft.EntityFrameworkCore;
namespace API.Helpers;

public class PagedList<T> : List<T>
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }

    public PagedList(IEnumerable<T> items, int count, int pageNumber, int pageSize)
    {
        CurrentPage = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        PageSize = pageSize;
        TotalCount = count;
        AddRange(items);
    }

    // ฟังก์ชันนี้จะรับหน้าที่ไปสั่ง .Skip() และ .Take() ที่ Database ให้เราอัตโนมัติ
    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> source, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        /*
        ⚠️ PERF-02: PagedList ทำ 2 Database Roundtrip (MEDIUM)
        ปัญหา:
            - ทุก Request ต้อง Query ไป Database 2 ครั้ง
            -ไม่มีปัญหาตอนข้อมูลน้อย แต่ COUNT(*) บน Table ใหญ่จะช้าขึ้นเรื่อยๆ
        แนะนำ (เมื่อ Scale):
            - ใช้ Cursor-based Pagination แทน (หาก Frontend รองรับ)
            - หรือ Cache count ไว้ช่วง 5-10 วินาทีด้วย IMemoryCache
            - ปัจจุบันยังรับได้สำหรับ Data ระดับหมื่น
        */
        var count = await source.CountAsync(ct);
        var items = await source.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedList<T>(items, count, pageNumber, pageSize);
    }
}
