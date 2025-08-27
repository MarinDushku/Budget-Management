// Budget Unit of Work Interface - Domain-Specific Transaction Coordination
// File: Shared/Data/IBudgetUnitOfWork.cs

using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Data.Repositories;

namespace BudgetManagement.Shared.Data
{
    /// <summary>
    /// Budget-specific Unit of Work interface that coordinates operations across Income, Spending, and Category entities
    /// Provides transaction management and ensures data consistency across domain operations
    /// </summary>
    public interface IBudgetUnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// Repository for Income entities
        /// </summary>
        IIncomeRepository Incomes { get; }

        /// <summary>
        /// Repository for Spending entities
        /// </summary>
        ISpendingRepository Spendings { get; }

        /// <summary>
        /// Repository for Category entities
        /// </summary>
        ICategoryRepository Categories { get; }

        // Domain-specific operations that span multiple repositories

        /// <summary>
        /// Gets budget summary data across Income and Spending
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Budget summary</returns>
        Task<Result<BudgetSummary>> GetBudgetSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets monthly summary data for a specific year
        /// </summary>
        /// <param name="year">Year to get summary for</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Monthly summaries</returns>
        Task<Result<IEnumerable<MonthlySummary>>> GetMonthlySummaryAsync(int year, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets bank statement summary based on statement day
        /// </summary>
        /// <param name="statementDay">Day of month for bank statement cycle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Bank statement summary</returns>
        Task<Result<BankStatementSummary>> GetBankStatementSummaryAsync(int statementDay, CancellationToken cancellationToken = default);

        /// <summary>
        /// Exports budget data to various formats
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="format">Export format</param>
        /// <param name="filePath">Optional file path for export</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Export result with file path</returns>
        Task<Result<ExportResult>> ExportDataAsync(DateTime startDate, DateTime endDate, ExportFormat format, string? filePath = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Imports budget data from a file
        /// </summary>
        /// <param name="filePath">Path to import file</param>
        /// <param name="format">Import format</param>
        /// <param name="importOptions">Import options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Import result</returns>
        Task<Result<ImportResult>> ImportDataAsync(string filePath, ImportFormat format, ImportOptions? importOptions = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs data cleanup operations (removes unused categories, duplicate entries, etc.)
        /// </summary>
        /// <param name="cleanupOptions">Cleanup options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Cleanup result</returns>
        Task<Result<CleanupResult>> PerformCleanupAsync(CleanupOptions? cleanupOptions = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets comprehensive statistics across all entities
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Comprehensive budget statistics</returns>
        Task<Result<BudgetStatistics>> GetBudgetStatisticsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates data integrity across all entities
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Data integrity validation result</returns>
        Task<Result<DataIntegrityResult>> ValidateDataIntegrityAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Archives old data based on retention policies
        /// </summary>
        /// <param name="archiveOptions">Archive options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Archive result</returns>
        Task<Result<ArchiveResult>> ArchiveOldDataAsync(ArchiveOptions archiveOptions, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Export format options
    /// </summary>
    public enum ExportFormat
    {
        Csv,
        Json,
        Excel,
        Pdf
    }

    /// <summary>
    /// Import format options
    /// </summary>
    public enum ImportFormat
    {
        Csv,
        Json,
        Excel
    }

    /// <summary>
    /// Export result data structure
    /// </summary>
    public class ExportResult
    {
        public string FilePath { get; set; } = string.Empty;
        public ExportFormat Format { get; set; }
        public long FileSizeBytes { get; set; }
        public int IncomeEntriesExported { get; set; }
        public int SpendingEntriesExported { get; set; }
        public int CategoriesExported { get; set; }
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ExportDuration { get; set; }
        public bool Success => !string.IsNullOrEmpty(FilePath);
    }

    /// <summary>
    /// Import options for data import
    /// </summary>
    public class ImportOptions
    {
        public bool SkipDuplicates { get; set; } = true;
        public bool CreateMissingCategories { get; set; } = true;
        public bool ValidateDataIntegrity { get; set; } = true;
        public bool BackupBeforeImport { get; set; } = true;
        public string? DefaultCategoryName { get; set; } = "Imported";
        public Dictionary<string, object> CustomOptions { get; set; } = new();
    }

    /// <summary>
    /// Import result data structure
    /// </summary>
    public class ImportResult
    {
        public ImportFormat Format { get; set; }
        public int IncomeEntriesImported { get; set; }
        public int SpendingEntriesImported { get; set; }
        public int CategoriesImported { get; set; }
        public int DuplicatesSkipped { get; set; }
        public int ErrorCount { get; set; }
        public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ImportDuration { get; set; }
        public IEnumerable<string> Errors { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> Warnings { get; set; } = Enumerable.Empty<string>();
        public bool Success => ErrorCount == 0;
        public string? BackupFilePath { get; set; }
    }

    /// <summary>
    /// Cleanup options for data cleanup operations
    /// </summary>
    public class CleanupOptions
    {
        public bool RemoveUnusedCategories { get; set; } = false; // Default to false for safety
        public bool RemoveDuplicateEntries { get; set; } = true;
        public bool FixDataIntegrityIssues { get; set; } = true;
        public bool ArchiveOldEntries { get; set; } = false;
        public DateTime? ArchiveBeforeDate { get; set; }
        public bool CreateBackupBeforeCleanup { get; set; } = true;
        public Dictionary<string, object> CustomOptions { get; set; } = new();
    }

    /// <summary>
    /// Cleanup result data structure
    /// </summary>
    public class CleanupResult
    {
        public int UnusedCategoriesRemoved { get; set; }
        public int DuplicateEntriesRemoved { get; set; }
        public int DataIntegrityIssuesFixed { get; set; }
        public int EntriesArchived { get; set; }
        public DateTime CleanupCompletedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan CleanupDuration { get; set; }
        public IEnumerable<string> Actions { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> Warnings { get; set; } = Enumerable.Empty<string>();
        public string? BackupFilePath { get; set; }
        public bool Success { get; set; } = true;
    }

    /// <summary>
    /// Comprehensive budget statistics
    /// </summary>
    public class BudgetStatistics
    {
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalSpending { get; set; }
        public decimal NetIncome => TotalIncome - TotalSpending;
        public decimal SavingsRate => TotalIncome > 0 ? ((TotalIncome - TotalSpending) / TotalIncome) * 100 : 0;
        public int IncomeEntries { get; set; }
        public int SpendingEntries { get; set; }
        public int TotalEntries => IncomeEntries + SpendingEntries;
        public int UniqueCategoriesUsed { get; set; }
        public decimal AverageIncomePerEntry { get; set; }
        public decimal AverageSpendingPerEntry { get; set; }
        public decimal AverageDailyIncome { get; set; }
        public decimal AverageDailySpending { get; set; }
        public CategoryWithSpendingStats TopSpendingCategory { get; set; } = new();
        public IEnumerable<MonthlyStatistics> MonthlyBreakdown { get; set; } = Enumerable.Empty<MonthlyStatistics>();
        public IEnumerable<CategoryWithSpendingStats> CategoryBreakdown { get; set; } = Enumerable.Empty<CategoryWithSpendingStats>();
        public int DaysWithIncome { get; set; }
        public int DaysWithSpending { get; set; }
        public int DaysWithActivity => Math.Max(DaysWithIncome, DaysWithSpending);
    }

    /// <summary>
    /// Monthly statistics for budget statistics
    /// </summary>
    public class MonthlyStatistics
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Income { get; set; }
        public decimal Spending { get; set; }
        public decimal NetIncome => Income - Spending;
        public decimal SavingsRate => Income > 0 ? ((Income - Spending) / Income) * 100 : 0;
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }

    /// <summary>
    /// Data integrity validation result
    /// </summary>
    public class DataIntegrityResult
    {
        public bool IsValid { get; set; } = true;
        public IEnumerable<string> Issues { get; set; } = Enumerable.Empty<string>();
        public IEnumerable<string> Warnings { get; set; } = Enumerable.Empty<string>();
        public int OrphanedSpendingEntries { get; set; }
        public int MissingCategories { get; set; }
        public int InvalidDateEntries { get; set; }
        public int InvalidAmountEntries { get; set; }
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public bool HasCriticalIssues => Issues.Any();
    }

    /// <summary>
    /// Archive options for archiving old data
    /// </summary>
    public class ArchiveOptions
    {
        public DateTime ArchiveBeforeDate { get; set; }
        public bool ArchiveIncome { get; set; } = true;
        public bool ArchiveSpending { get; set; } = true;
        public bool CreateBackup { get; set; } = true;
        public string? ArchiveFilePath { get; set; }
        public bool CompressArchive { get; set; } = true;
        public Dictionary<string, object> CustomOptions { get; set; } = new();
    }

    /// <summary>
    /// Archive result data structure
    /// </summary>
    public class ArchiveResult
    {
        public int IncomeEntriesArchived { get; set; }
        public int SpendingEntriesArchived { get; set; }
        public int TotalEntriesArchived => IncomeEntriesArchived + SpendingEntriesArchived;
        public string? ArchiveFilePath { get; set; }
        public long ArchiveFileSizeBytes { get; set; }
        public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
        public TimeSpan ArchiveDuration { get; set; }
        public string? BackupFilePath { get; set; }
        public bool Success { get; set; } = true;
        public IEnumerable<string> Warnings { get; set; } = Enumerable.Empty<string>();
    }
}