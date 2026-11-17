using Ewallet.Dtos.Common;
using Microsoft.EntityFrameworkCore;

namespace Ewallet.Repositories;

public static class QueryableExtensions
{
  public static async Task<PagedResponse<T>> ToPagedAsync<T>(this IQueryable<T> query, int page, int pageSize, CancellationToken ct)
  {
    page = Math.Max(1, page);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var total = await query.CountAsync(ct);
    var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
    return new PagedResponse<T>(items, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
  }

  /// <summary>Date-only upper bound (00:00:00) means "until the end of that day".</summary>
  public static (DateTime value, bool exclusive) UpperBound(DateTime to)
    => to.TimeOfDay == TimeSpan.Zero ? (to.Date.AddDays(1), true) : (to, false);
}
