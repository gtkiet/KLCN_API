using Microsoft.EntityFrameworkCore;
using KLCN_API.Models.DTOs.Response;

namespace KLCN_API.Helpers;

public static class PaginationHelper
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Phân trang IQueryable, thực thi 2 query: Count + dữ liệu trang.
    /// </summary>
    public static async Task<PagedResponse<T>> ToPagedAsync<T>(
        IQueryable<T> query,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Phân trang từ List đã có trong bộ nhớ (dùng khi đã fetch xong).
    /// </summary>
    public static PagedResponse<T> ToPagedFromList<T>(
        IEnumerable<T> source,
        int page,
        int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var list = source.ToList();
        var totalCount = list.Count;
        var items = list
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResponse<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
}