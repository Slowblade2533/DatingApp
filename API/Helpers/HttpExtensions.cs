using System.Text.Json;

namespace API.Helpers;

public static class HttpExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static void AddPaginationHeader<T>(this HttpResponse response, PagedList<T> data)
    {
        var paginationHeader = new PaginationHeader(
            data.CurrentPage,
            data.PageSize,
            data.TotalCount,
            data.TotalPages);

        response.Headers["Pagination"] = JsonSerializer.Serialize(paginationHeader, JsonOptions);
    }

    private sealed record PaginationHeader(
        int CurrentPage,
        int ItemsPerPage,
        int TotalItems,
        int TotalPages);
}
