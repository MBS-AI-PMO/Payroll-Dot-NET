using Microsoft.EntityFrameworkCore;

namespace PayslipMgtSystem.Helper
{
    public static class PaginationExtensions
    {
        /// <summary>Number of pages needed to show every row of <paramref name="queryable"/>.</summary>
        public static async Task<int> PageCountAsync<T>(this IQueryable<T> queryable, int recordsPerPage)
        {
            var count = await queryable.CountAsync();
            return (int)Math.Ceiling(count / (double)recordsPerPage);
        }

        public static IQueryable<T> Paginate<T>(this IQueryable<T> queryable, PaginationDTO pagination)
        {
            return queryable
                .Skip((pagination.Page - 1) * pagination.QuantityPerPage)
                .Take(pagination.QuantityPerPage);
        }
    }
}
