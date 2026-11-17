using System.ComponentModel.DataAnnotations;

namespace Ewallet.Dtos.Common;

public class PageQuery
{
  [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
  [Range(1, 100)] public int PageSize { get; set; } = 10;
}

public record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems, int TotalPages);
