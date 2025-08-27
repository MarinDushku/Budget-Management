// Repository Pattern Interface - Modern Data Access Abstraction
// File: Shared/Infrastructure/IRepository.cs

using System.Linq.Expressions;
using BudgetManagement.Shared.Core;

namespace BudgetManagement.Shared.Infrastructure
{
    /// <summary>
    /// Generic repository interface for data access operations
    /// Provides common CRUD operations with modern Result pattern
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        // Query operations
        Task<Result<T?>> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<T>>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<T>>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<Result<T?>> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<Result<bool>> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<Result<int>> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

        // Command operations
        Task<Result<T>> AddAsync(T entity, CancellationToken cancellationToken = default);
        Task<Result<IEnumerable<T>>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
        Task<Result<T>> UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
        Task<Result> DeleteAsync(T entity, CancellationToken cancellationToken = default);
        Task<Result> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        // Pagination support
        Task<Result<PagedResult<T>>> GetPagedAsync(
            int page = 1, 
            int pageSize = 20, 
            Expression<Func<T, bool>>? predicate = null,
            Expression<Func<T, object>>? orderBy = null,
            bool ascending = true,
            CancellationToken cancellationToken = default);

        // Specification pattern support
        Task<Result<IEnumerable<T>>> FindAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
        Task<Result<int>> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Unit of Work pattern interface for managing transactions
    /// Provides atomic operations across multiple repositories
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        // Repository accessors
        IRepository<TEntity> Repository<TEntity>() where TEntity : class;

        // Transaction management
        Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<Result> BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task<Result> CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task<Result> RollbackTransactionAsync(CancellationToken cancellationToken = default);

        // Bulk operations
        Task<Result<int>> ExecuteSqlAsync(string sql, object[] parameters, CancellationToken cancellationToken = default);
        Task<Result> TruncateTableAsync<TEntity>() where TEntity : class;
    }

    /// <summary>
    /// Specification pattern interface for encapsulating query logic
    /// Allows for reusable and testable query specifications
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface ISpecification<T>
    {
        /// <summary>
        /// Criteria for filtering entities
        /// </summary>
        Expression<Func<T, bool>>? Criteria { get; }

        /// <summary>
        /// Include expressions for related entities
        /// </summary>
        List<Expression<Func<T, object>>> Includes { get; }

        /// <summary>
        /// Include expressions for related entities as strings
        /// </summary>
        List<string> IncludeStrings { get; }

        /// <summary>
        /// Order by expression
        /// </summary>
        Expression<Func<T, object>>? OrderBy { get; }

        /// <summary>
        /// Order by descending expression
        /// </summary>
        Expression<Func<T, object>>? OrderByDescending { get; }

        /// <summary>
        /// Group by expression
        /// </summary>
        Expression<Func<T, object>>? GroupBy { get; }

        /// <summary>
        /// Number of items to take
        /// </summary>
        int? Take { get; }

        /// <summary>
        /// Number of items to skip
        /// </summary>
        int? Skip { get; }

        /// <summary>
        /// Whether to track entities in the context
        /// </summary>
        bool IsSplitQuery { get; }
    }

    /// <summary>
    /// Base specification class providing common functionality
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public abstract class BaseSpecification<T> : ISpecification<T>
    {
        protected BaseSpecification(Expression<Func<T, bool>>? criteria = null)
        {
            Criteria = criteria;
        }

        public Expression<Func<T, bool>>? Criteria { get; private set; }
        public List<Expression<Func<T, object>>> Includes { get; } = new();
        public List<string> IncludeStrings { get; } = new();
        public Expression<Func<T, object>>? OrderBy { get; private set; }
        public Expression<Func<T, object>>? OrderByDescending { get; private set; }
        public Expression<Func<T, object>>? GroupBy { get; private set; }
        public int? Take { get; private set; }
        public int? Skip { get; private set; }
        public bool IsSplitQuery { get; private set; }

        protected virtual void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        protected virtual void AddInclude(string includeString)
        {
            IncludeStrings.Add(includeString);
        }

        protected virtual void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
        }

        protected virtual void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
        {
            OrderByDescending = orderByDescendingExpression;
        }

        protected virtual void ApplyGroupBy(Expression<Func<T, object>> groupByExpression)
        {
            GroupBy = groupByExpression;
        }

        protected virtual void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
        }

        protected virtual void ApplySplitQuery()
        {
            IsSplitQuery = true;
        }
    }
}