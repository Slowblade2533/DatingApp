using System.Text.Json;
namespace API.Helpers;

public static class HttpExtensions
{
    // ✅ สร้างครั้งเดียว reuse ตลอด
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // สร้าง Extension Method สำหรับโยนค่าใส่ Response Header
    public static void AddPaginationHeader<T>(this HttpResponse response, PagedList<T> data)
    {
        var paginationHeader = new
        {
            currentPage = data.CurrentPage,
            itemsPerPage = data.PageSize,
            totalItems = data.TotalCount,
            totalPages = data.TotalPages
        };

        response.Headers.Append("Pagination", JsonSerializer.Serialize(paginationHeader, _jsonOptions));
    }
}
