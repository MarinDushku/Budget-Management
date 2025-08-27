// CQRS Query Infrastructure - Modern Query Pattern Implementation
// File: Shared/Core/Queries.cs

using MediatR;

namespace BudgetManagement.Shared.Core
{
    /// <summary>
    /// Base interface for all queries (operations that read data)
    /// Queries represent requests for information without modifying state
    /// </summary>
    /// <typeparam name="TResult">Type of the result returned by the query</typeparam>
    public interface IQuery<TResult> : IRequest<Result<TResult>>
    {
        /// <summary>
        /// Unique identifier for tracking query execution
        /// </summary>
        Guid QueryId { get; }

        /// <summary>
        /// Timestamp when the query was created
        /// </summary>
        DateTime CreatedAt { get; }
    }

    /// <summary>
    /// Base abstract class for queries providing common properties
    /// </summary>
    /// <typeparam name="TResult">Type of the result returned by the query</typeparam>
    public abstract record BaseQuery<TResult> : IQuery<TResult>
    {
        public Guid QueryId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Base interface for query handlers
    /// </summary>
    /// <typeparam name="TQuery">Type of query being handled</typeparam>
    /// <typeparam name="TResult">Type of the result returned</typeparam>
    public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, Result<TResult>>
        where TQuery : IQuery<TResult>
    {
    }

    /// <summary>
    /// Abstract base class for query handlers providing common functionality
    /// </summary>
    /// <typeparam name="TQuery">Type of query being handled</typeparam>
    /// <typeparam name="TResult">Type of the result returned</typeparam>
    public abstract class BaseQueryHandler<TQuery, TResult> : IQueryHandler<TQuery, TResult>
        where TQuery : IQuery<TResult>
    {
        /// <summary>
        /// Handles the query execution
        /// </summary>
        /// <param name="request">The query to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result with the requested data</returns>
        public abstract Task<Result<TResult>> Handle(TQuery request, CancellationToken cancellationToken);

        /// <summary>
        /// Validates the query before execution
        /// Override to provide custom validation logic
        /// </summary>
        /// <param name="query">Query to validate</param>
        /// <returns>Validation result</returns>
        protected virtual Result ValidateQuery(TQuery query)
        {
            if (query == null)
                return Result.Failure(Error.Validation(Error.Codes.VALIDATION_FAILED, "Query cannot be null"));

            return Result.Success();
        }

        /// <summary>
        /// Logs query execution start
        /// </summary>
        /// <param name="query">Query being executed</param>
        protected virtual void LogQueryStart(TQuery query)
        {
            System.Diagnostics.Debug.WriteLine($"Executing query: {query.GetType().Name} [{query.QueryId}]");
        }

        /// <summary>
        /// Logs query execution completion
        /// </summary>
        /// <param name="query">Query that was executed</param>
        /// <param name="result">Result of the execution</param>
        protected virtual void LogQueryComplete(TQuery query, IResult result)
        {
            var status = result.IsSuccess ? "SUCCESS" : "FAILURE";
            System.Diagnostics.Debug.WriteLine($"Query completed: {query.GetType().Name} [{query.QueryId}] - {status}");
            
            if (result.IsFailure && result.Error != null)
            {
                System.Diagnostics.Debug.WriteLine($"Query error: {result.Error}");
            }
        }
    }

    /// <summary>
    /// Base class for paginated queries
    /// </summary>
    /// <typeparam name="TResult">Type of items in the paginated result</typeparam>
    public abstract record BasePaginatedQuery<TResult> : BaseQuery<PagedResult<TResult>>
    {
        /// <summary>
        /// Page number (1-based)
        /// </summary>
        public int Page { get; init; } = 1;

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; init; } = 20;

        /// <summary>
        /// Optional search term
        /// </summary>
        public string? SearchTerm { get; init; }

        /// <summary>
        /// Optional sort field
        /// </summary>
        public string? SortBy { get; init; }

        /// <summary>
        /// Sort direction (true for ascending, false for descending)
        /// </summary>
        public bool SortAscending { get; init; } = true;

        /// <summary>
        /// Calculate the number of items to skip
        /// </summary>
        public int Skip => Math.Max(0, (Page - 1) * PageSize);

        /// <summary>
        /// Validates pagination parameters
        /// </summary>
        public virtual Result ValidatePagination()
        {
            if (Page < 1)
                return Result.Failure(Error.Validation(Error.Codes.VALIDATION_FAILED, "Page must be greater than 0"));

            if (PageSize < 1 || PageSize > 100)
                return Result.Failure(Error.Validation(Error.Codes.VALIDATION_FAILED, "PageSize must be between 1 and 100"));

            return Result.Success();
        }
    }

    /// <summary>
    /// Represents a paginated result set
    /// </summary>
    /// <typeparam name="T">Type of items in the result</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// Items in the current page
        /// </summary>
        public IReadOnlyList<T> Items { get; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int Page { get; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages { get; }

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => Page > 1;

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage => Page < TotalPages;

        public PagedResult(IReadOnlyList<T> items, int totalCount, int page, int pageSize)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            TotalCount = totalCount;
            Page = page;
            PageSize = pageSize;
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        }

        /// <summary>
        /// Creates an empty paged result
        /// </summary>
        public static PagedResult<T> Empty(int page = 1, int pageSize = 20)
            => new(Array.Empty<T>(), 0, page, pageSize);

        /// <summary>
        /// Creates a paged result from a full list and pagination parameters
        /// </summary>
        public static PagedResult<T> Create(IEnumerable<T> allItems, int page, int pageSize)
        {
            var items = allItems.ToList();
            var totalCount = items.Count;
            var skip = Math.Max(0, (page - 1) * pageSize);
            var pagedItems = items.Skip(skip).Take(pageSize).ToList();

            return new PagedResult<T>(pagedItems, totalCount, page, pageSize);
        }
    }

    /// <summary>
    /// Common date range query parameters
    /// </summary>
    public record DateRangeQuery
    {
        /// <summary>
        /// Start date of the range (inclusive)
        /// </summary>
        public DateTime StartDate { get; init; }

        /// <summary>
        /// End date of the range (inclusive)
        /// </summary>
        public DateTime EndDate { get; init; }

        /// <summary>
        /// Validates the date range
        /// </summary>
        public virtual Result ValidateDateRange()
        {
            if (StartDate > EndDate)
                return Result.Failure(Error.Validation(Error.Codes.INVALID_DATE, "Start date cannot be after end date"));

            if (EndDate > DateTime.UtcNow.Date.AddDays(1))
                return Result.Failure(Error.Validation(Error.Codes.INVALID_DATE, "End date cannot be in the future"));

            var daysDifference = (EndDate - StartDate).Days;
            if (daysDifference > 365)
                return Result.Failure(Error.Validation(Error.Codes.INVALID_DATE, "Date range cannot exceed 365 days"));

            return Result.Success();
        }

        /// <summary>
        /// Gets the current month date range
        /// </summary>
        public static DateRangeQuery CurrentMonth()
        {
            var now = DateTime.UtcNow.Date;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            return new DateRangeQuery
            {
                StartDate = startOfMonth,
                EndDate = endOfMonth
            };
        }

        /// <summary>
        /// Gets the current year date range
        /// </summary>
        public static DateRangeQuery CurrentYear()
        {
            var now = DateTime.UtcNow.Date;
            var startOfYear = new DateTime(now.Year, 1, 1);
            var endOfYear = new DateTime(now.Year, 12, 31);

            return new DateRangeQuery
            {
                StartDate = startOfYear,
                EndDate = endOfYear
            };
        }

        /// <summary>
        /// Gets the last N days date range
        /// </summary>
        public static DateRangeQuery LastDays(int days)
        {
            var endDate = DateTime.UtcNow.Date;
            var startDate = endDate.AddDays(-days + 1);

            return new DateRangeQuery
            {
                StartDate = startDate,
                EndDate = endDate
            };
        }
    }
}