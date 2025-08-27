// Unit of Work Pattern Interface - Transaction Coordination
// File: Shared/Data/IUnitOfWork.cs

using BudgetManagement.Shared.Core;

namespace BudgetManagement.Shared.Data
{
    /// <summary>
    /// Unit of Work interface for coordinating multiple repository operations
    /// Manages transactions and ensures data consistency across multiple entities
    /// </summary>
    public interface IUnitOfWork : IDisposable, IAsyncDisposable
    {
        // Repository access - will be implemented by specific Unit of Work implementations
        // These properties will be defined in the concrete implementation based on domain entities

        /// <summary>
        /// Saves all pending changes to the data store
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating success or failure</returns>
        Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a new transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result with transaction object</returns>
        Task<Result<IUnitOfWorkTransaction>> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Discards all pending changes
        /// </summary>
        void DiscardChanges();

        /// <summary>
        /// Checks if there are any pending changes
        /// </summary>
        bool HasChanges { get; }

        /// <summary>
        /// Gets the connection state
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Tests the connection to the data store
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating connection status</returns>
        Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Initializes the data store schema
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating initialization success</returns>
        Task<Result> InitializeAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Transaction interface for Unit of Work pattern
    /// </summary>
    public interface IUnitOfWorkTransaction : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Transaction ID for tracking
        /// </summary>
        Guid TransactionId { get; }

        /// <summary>
        /// Transaction state
        /// </summary>
        TransactionState State { get; }

        /// <summary>
        /// Commits the transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating commit success</returns>
        Task<Result> CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the transaction
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating rollback success</returns>
        Task<Result> RollbackAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a savepoint within the transaction
        /// </summary>
        /// <param name="savepointName">Name of the savepoint</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result with savepoint</returns>
        Task<Result<ITransactionSavepoint>> CreateSavepointAsync(string savepointName, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Transaction savepoint interface
    /// </summary>
    public interface ITransactionSavepoint : IDisposable
    {
        /// <summary>
        /// Savepoint name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Rolls back to this savepoint
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating rollback success</returns>
        Task<Result> RollbackToAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Releases this savepoint
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result indicating release success</returns>
        Task<Result> ReleaseAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Transaction state enumeration
    /// </summary>
    public enum TransactionState
    {
        Active,
        Committed,
        RolledBack,
        Failed
    }

    /// <summary>
    /// Unit of Work factory for creating Unit of Work instances
    /// </summary>
    public interface IUnitOfWorkFactory
    {
        /// <summary>
        /// Creates a new Unit of Work instance
        /// </summary>
        /// <returns>Unit of Work instance</returns>
        Task<Result<IUnitOfWork>> CreateAsync();
    }

    /// <summary>
    /// Abstract base class for Unit of Work implementations
    /// Provides common functionality and ensures consistent behavior
    /// </summary>
    public abstract class BaseUnitOfWork : IUnitOfWork
    {
        private bool _disposed;
        private readonly object _lock = new object();

        /// <summary>
        /// Current transaction
        /// </summary>
        protected IUnitOfWorkTransaction? CurrentTransaction { get; set; }

        /// <inheritdoc/>
        public abstract bool HasChanges { get; }

        /// <inheritdoc/>
        public abstract bool IsConnected { get; }

        /// <inheritdoc/>
        public abstract Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract Task<Result<IUnitOfWorkTransaction>> BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract void DiscardChanges();

        /// <inheritdoc/>
        public abstract Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract Task<Result> InitializeAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs the actual dispose logic
        /// </summary>
        /// <param name="disposing">Whether disposing from Dispose() or finalizer</param>
        protected virtual async ValueTask DisposeAsyncCore(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    if (CurrentTransaction != null)
                    {
                        await CurrentTransaction.DisposeAsync();
                        CurrentTransaction = null;
                    }
                }

                _disposed = true;
            }
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            DisposeAsync().AsTask().Wait();
        }

        /// <summary>
        /// Throws if the Unit of Work has been disposed
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }
        }

        /// <summary>
        /// Executes an operation within a transaction
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        public async Task<Result<T>> ExecuteInTransactionAsync<T>(
            Func<IUnitOfWorkTransaction, CancellationToken, Task<Result<T>>> operation,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            var transactionResult = await BeginTransactionAsync(cancellationToken);
            if (transactionResult.IsFailure)
            {
                return Result<T>.Failure(transactionResult.Error!);
            }

            var transaction = transactionResult.Value!;
            try
            {
                var result = await operation(transaction, cancellationToken);
                
                if (result.IsSuccess)
                {
                    var commitResult = await transaction.CommitAsync(cancellationToken);
                    if (commitResult.IsFailure)
                    {
                        return Result<T>.Failure(commitResult.Error!);
                    }
                    return result;
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return result;
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<T>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, 
                    "Transaction failed with unexpected error", new Dictionary<string, object> { ["Exception"] = ex }));
            }
            finally
            {
                await transaction.DisposeAsync();
            }
        }

        /// <summary>
        /// Executes an operation within a transaction (non-generic version)
        /// </summary>
        /// <param name="operation">Operation to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        public async Task<Result> ExecuteInTransactionAsync(
            Func<IUnitOfWorkTransaction, CancellationToken, Task<Result>> operation,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteInTransactionAsync<object>(async (tx, ct) =>
            {
                var result = await operation(tx, ct);
                return result.IsSuccess 
                    ? Result<object>.Success(new object()) 
                    : Result<object>.Failure(result.Error!);
            }, cancellationToken);
        }
    }

    /// <summary>
    /// Extension methods for Unit of Work
    /// </summary>
    public static class UnitOfWorkExtensions
    {
        /// <summary>
        /// Saves changes and returns the result
        /// </summary>
        /// <param name="unitOfWork">Unit of Work instance</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of entities saved</returns>
        public static async Task<Result<int>> SaveAndCountAsync(this IUnitOfWork unitOfWork, CancellationToken cancellationToken = default)
        {
            var saveResult = await unitOfWork.SaveChangesAsync(cancellationToken);
            if (saveResult.IsFailure)
            {
                return Result<int>.Failure(saveResult.Error!);
            }

            // Note: Actual count would need to be tracked by the implementation
            return Result<int>.Success(0); // Placeholder - actual implementation would track changes
        }

        /// <summary>
        /// Executes multiple operations in a single transaction
        /// </summary>
        /// <param name="unitOfWork">Unit of Work instance</param>
        /// <param name="operations">Operations to execute</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Results of all operations</returns>
        public static async Task<Result<IEnumerable<Result>>> ExecuteMultipleAsync(
            this IUnitOfWork unitOfWork,
            IEnumerable<Func<CancellationToken, Task<Result>>> operations,
            CancellationToken cancellationToken = default)
        {
            var results = new List<Result>();
            
            return await ((BaseUnitOfWork)unitOfWork).ExecuteInTransactionAsync(async (tx, ct) =>
            {
                foreach (var operation in operations)
                {
                    var result = await operation(ct);
                    results.Add(result);
                    
                    if (result.IsFailure)
                    {
                        return Result<IEnumerable<Result>>.Failure(result.Error!);
                    }
                }

                return Result<IEnumerable<Result>>.Success(results.AsEnumerable());
            }, cancellationToken);
        }
    }
}