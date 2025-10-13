using System;
using System.Linq;

namespace 專題MVC修正.Models.DTOs
{
    public class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public IQueryable<T> Query { get; set; }
    }

    public static class PagingExtensions
    {
        public static IQueryable<T> Paginate<T>(this IQueryable<T> q, int page, int pageSize)
        {
            if (page < 1) page = 1;
            return q.Skip((page - 1) * pageSize).Take(pageSize);
        }
    }
}
