// Generic Repository Interface - Data Access Abstraction
// File: Shared/Data/IRepository.cs

using System.Linq.Expressions;
using BudgetManagement.Shared.Core;

namespace BudgetManagement.Shared.Data
{
    /// <summary>
    /// Generic repository interface providing common data access operations
    /// Implements the Repository pattern for consistent data access across entities
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        // Query operations
        Task<Result<T?>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<T>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<T>>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<Result<T?>> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<Result<int>> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
        Task<Result<bool>> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        // Command operations
        Task<Result<T>> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<T>>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<Result<T>> UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(T entity, CancellationToken cancellationToken = default);
        Task<Result> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        // Pagination support
        Task<Result<PagedResult<T>>> GetPagedAsync(
            int pageNumber, 
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false,
            CancellationToken cancellationToken = default);

        // Aggregation operations
        Task<Result<decimal>> SumAsync<TProperty>(
            Expression<Func<T, TProperty>> selector,
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default) where TProperty : struct;

        Task<Result<TProperty?>> MaxAsync<TProperty>(
            Expression<Func<T, TProperty>> selector,
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default);

        Task<Result<TProperty?>> MinAsync<TProperty>(
            Expression<Func<T, TProperty>> selector,
            Expression<Func<T, bool>>? predicate = null,
            CancellationToken cancellationToken = default);

        // Advanced query operations
        Task<Result<IEnumerable<T>>> GetWithIncludeAsync(
            Expression<Func<T, bool>>? predicate = null,
            params Expression<Func<T, object>>[] includes);
    }

    /// <summary>
    /// Read-only repository interface for scenarios where only read operations are needed
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IReadOnlyRepository<T> where T : class
    {
        Task<Result<T?>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<T>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<T>>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<Result<T?>> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<Result<int>> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);
        Task<Result<bool>> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        
        Task<Result<PagedResult<T>>> GetPagedAsync(
            int pageNumber, 
            int pageSize,
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null,
            bool descending = false,
            CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Paged result wrapper for paginated queries
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;

        public PagedResult()
        {
        }

        public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }

        /// <summary>
        /// Creates an empty paged result
        /// </summary>
        public static PagedResult<T> Empty(int pageNumber, int pageSize) =>
            new(Enumerable.Empty<T>(), 0, pageNumber, pageSize);

        /// <summary>
        /// Creates a paged result with items
        /// </summary>
        public static PagedResult<T> Create(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize) =>
            new(items, totalCount, pageNumber, pageSize);
    }

    /// <summary>
    /// Repository configuration options
    /// </summary>
    public class RepositoryOptions
    {
        /// <summary>
        /// Default page size for paginated queries
        /// </summary>
        public int DefaultPageSize { get; set; } = 50;

        /// <summary>
        /// Maximum page size to prevent excessive memory usage
        /// </summary>
        public int MaxPageSize { get; set; } = 1000;

        /// <summary>
        /// Query timeout in seconds
        /// </summary>
        public int QueryTimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Whether to enable query result caching
        /// </summary>
        public bool EnableCaching { get; set; } = false;

        /// <summary>
        /// Cache duration for query results in minutes
        /// </summary>
        public int CacheDurationMinutes { get; set; } = 5;
    }

    /// <summary>
    /// Extensions for repository operations
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Gets entities with date range filtering
        /// </summary>
        public static async Task<Result<IEnumerable<T>>> GetByDateRangeAsync<T>(
            this IRepository<T> repository,
            Expression<Func<T, DateTime>> dateSelector,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default) where T : class
        {
            // Build predicate for date range
            var parameter = dateSelector.Parameters[0];
            var dateProperty = dateSelector.Body;
            
            var startConstant = Expression.Constant(startDate);
            var endConstant = Expression.Constant(endDate);
            
            var startComparison = Expression.GreaterThanOrEqual(dateProperty, startConstant);
            var endComparison = Expression.LessThanOrEqual(dateProperty, endConstant);
            var rangeComparison = Expression.AndAlso(startComparison, endComparison);
            
            var predicate = Expression.Lambda<Func<T, bool>>(rangeComparison, parameter);
            
            return await repository.FindAsync(predicate, cancellationToken);
        }

        /// <summary>
        /// Gets the most recent entities
        /// </summary>
        public static async Task<Result<IEnumerable<T>>> GetMostRecentAsync<T>(
            this IRepository<T> repository,
            Expression<Func<T, DateTime>> dateSelector,
            int count,
            CancellationToken cancellationToken = default) where T : class
        {
            // Convert DateTime expression to object expression
            var parameter = dateSelector.Parameters[0];
            var member = dateSelector.Body;
            var convertedExpression = Expression.Lambda<Func<T, object>>(
                Expression.Convert(member, typeof(object)), parameter);

            var result = await repository.GetPagedAsync(
                pageNumber: 1,
                pageSize: count,
                orderBy: convertedExpression,
                descending: true,
                cancellationToken: cancellationToken);

            return result.IsSuccess 
                ? Result<IEnumerable<T>>.Success(result.Value!.Items)
                : Result<IEnumerable<T>>.Failure(result.Error!);
        }
    }
}