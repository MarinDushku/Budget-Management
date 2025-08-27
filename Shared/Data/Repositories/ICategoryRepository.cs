// Category Repository Interface - Domain-Specific Data Access
// File: Shared/Data/Repositories/ICategoryRepository.cs

using BudgetManagement.Models;
using BudgetManagement.Shared.Core;

namespace BudgetManagement.Shared.Data.Repositories
{
    /// <summary>
    /// Repository interface for Category entity with domain-specific operations
    /// Extends the generic repository with category-specific queries and operations
    /// </summary>
    public interface ICategoryRepository : IRepository<Category>
    {
        /// <summary>
        /// Gets all active categories
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Active categories</returns>
        Task<Result<IEnumerable<Category>>> GetActiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all inactive categories
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Inactive categories</returns>
        Task<Result<IEnumerable<Category>>> GetInactiveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets categories by name pattern
        /// </summary>
        /// <param name="namePattern">Name pattern to search for</param>
        /// <param name="includeInactive">Whether to include inactive categories</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Categories matching the name pattern</returns>
        Task<Result<IEnumerable<Category>>> GetByNamePatternAsync(string namePattern, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets category by exact name
        /// </summary>
        /// <param name="name">Category name</param>
        /// <param name="includeInactive">Whether to include inactive categories</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Category with the specified name</returns>
        Task<Result<Category?>> GetByNameAsync(string name, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets categories with spending statistics for a date range
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="includeInactive">Whether to include inactive categories</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Categories with spending statistics</returns>
        Task<Result<IEnumerable<CategoryWithSpendingStats>>> GetWithSpendingStatsAsync(DateTime startDate, DateTime endDate, bool includeInactive = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets categories that have been used (have spending entries)
        /// </summary>
        /// <param name="startDate">Start date (optional)</param>
        /// <param name="endDate">End date (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Categories that have been used</returns>
        Task<Result<IEnumerable<Category>>> GetUsedCategoriesAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets categories that have never been used
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Unused categories</returns>
        Task<Result<IEnumerable<Category>>> GetUnusedCategoriesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the most frequently used categories
        /// </summary>
        /// <param name="count">Number of categories to retrieve</param>
        /// <param name="startDate">Start date (optional)</param>
        /// <param name="endDate">End date (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Most frequently used categories</returns>
        Task<Result<IEnumerable<CategoryUsageStats>>> GetMostUsedAsync(int count = 10, DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets categories ordered by total spending amount
        /// </summary>
        /// <param name="startDate">Start date (inclusive)</param>
        /// <param name="endDate">End date (inclusive)</param>
        /// <param name="descending">Whether to order by highest spending first</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Categories ordered by spending amount</returns>
        Task<Result<IEnumerable<CategoryWithSpendingStats>>> GetOrderedBySpendingAsync(DateTime startDate, DateTime endDate, bool descending = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a category name is already taken
        /// </summary>
        /// <param name="name">Category name to check</param>
        /// <param name="excludeId">Category ID to exclude from the check (for updates)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the name is already taken</returns>
        Task<Result<bool>> IsNameTakenAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Soft deletes a category (marks as inactive)
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        Task<Result> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Restores a soft-deleted category (marks as active)
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        Task<Result> RestoreAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a category can be safely deleted (has no spending entries)
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if the category can be safely deleted</returns>
        Task<Result<bool>> CanBeDeletedAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets category usage summary
        /// </summary>
        /// <param name="id">Category ID</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Category usage summary</returns>
        Task<Result<CategoryUsageSummary>> GetUsageSummaryAsync(int id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk activate categories
        /// </summary>
        /// <param name="categoryIds">Category IDs to activate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of categories activated</returns>
        Task<Result<int>> BulkActivateAsync(IEnumerable<int> categoryIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bulk deactivate categories
        /// </summary>
        /// <param name="categoryIds">Category IDs to deactivate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Number of categories deactivated</returns>
        Task<Result<int>> BulkDeactivateAsync(IEnumerable<int> categoryIds, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Category with spending statistics data structure
    /// </summary>
    public class CategoryWithSpendingStats : Category
    {
        public decimal TotalSpending { get; set; }
        public int SpendingEntryCount { get; set; }
        public decimal AverageSpending { get; set; }
        public decimal MinSpending { get; set; }
        public decimal MaxSpending { get; set; }
        public DateTime? FirstSpendingDate { get; set; }
        public DateTime? LastSpendingDate { get; set; }
        public decimal PercentageOfTotalSpending { get; set; }
        public int DaysWithSpending { get; set; }
    }

    /// <summary>
    /// Category usage statistics data structure
    /// </summary>
    public class CategoryUsageStats
    {
        public Category Category { get; set; } = new();
        public int UsageCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public DateTime FirstUsed { get; set; }
        public DateTime LastUsed { get; set; }
        public int DaysSinceLastUsed => (DateTime.Now.Date - LastUsed.Date).Days;
        public decimal UsageFrequencyPerMonth { get; set; }
        public bool IsRegularlyUsed => UsageFrequencyPerMonth > 1;
    }

    /// <summary>
    /// Category usage summary data structure
    /// </summary>
    public class CategoryUsageSummary
    {
        public Category Category { get; set; } = new();
        public int TotalSpendingEntries { get; set; }
        public decimal TotalSpendingAmount { get; set; }
        public DateTime? FirstSpendingDate { get; set; }
        public DateTime? LastSpendingDate { get; set; }
        public decimal AverageMonthlySpending { get; set; }
        public decimal AverageTransactionAmount { get; set; }
        public int MonthsWithActivity { get; set; }
        public IEnumerable<MonthlyUsage> MonthlyBreakdown { get; set; } = Enumerable.Empty<MonthlyUsage>();
        public bool HasAnySpending => TotalSpendingEntries > 0;
        public bool IsRecentlyUsed => LastSpendingDate?.Date >= DateTime.Now.Date.AddDays(-30);
        public bool IsActiveCategory => Category.IsActive;
    }

    /// <summary>
    /// Monthly usage data for category usage summary
    /// </summary>
    public class MonthlyUsage
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int EntryCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public string MonthName => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    }

    /// <summary>
    /// Extension methods for category repository operations
    /// </summary>
    public static class CategoryRepositoryExtensions
    {
        /// <summary>
        /// Gets categories suitable for a dropdown/selection list
        /// </summary>
        /// <param name="repository">Category repository</param>
        /// <param name="includeUsageStats">Whether to include usage statistics</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Categories for selection</returns>
        public static async Task<Result<IEnumerable<CategorySelectionItem>>> GetForSelectionAsync(
            this ICategoryRepository repository,
            bool includeUsageStats = false,
            CancellationToken cancellationToken = default)
        {
            var categoriesResult = await repository.GetActiveAsync(cancellationToken);
            if (categoriesResult.IsFailure)
            {
                return Result<IEnumerable<CategorySelectionItem>>.Failure(categoriesResult.Error!);
            }

            var categories = categoriesResult.Value!;
            var selectionItems = new List<CategorySelectionItem>();

            foreach (var category in categories)
            {
                var item = new CategorySelectionItem
                {
                    Id = category.Id,
                    Name = category.Name,
                    Icon = category.Icon,
                    IsActive = category.IsActive
                };

                if (includeUsageStats)
                {
                    var usageStatsResult = await repository.GetMostUsedAsync(int.MaxValue, cancellationToken: cancellationToken);
                    if (usageStatsResult.IsSuccess)
                    {
                        var stats = usageStatsResult.Value!.FirstOrDefault(u => u.Category.Id == category.Id);
                        if (stats != null)
                        {
                            item.UsageCount = stats.UsageCount;
                            item.IsFrequentlyUsed = stats.IsRegularlyUsed;
                        }
                    }
                }

                selectionItems.Add(item);
            }

            // Order by usage frequency if stats are available, otherwise alphabetically
            var orderedItems = includeUsageStats
                ? selectionItems.OrderByDescending(i => i.UsageCount).ThenBy(i => i.Name)
                : selectionItems.OrderBy(i => i.Name);

            return Result<IEnumerable<CategorySelectionItem>>.Success(orderedItems);
        }
    }

    /// <summary>
    /// Category selection item for UI dropdowns
    /// </summary>
    public class CategorySelectionItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public bool IsActive { get; set; }
        public int UsageCount { get; set; }
        public bool IsFrequentlyUsed { get; set; }
        public string DisplayName => IsFrequentlyUsed ? $"{Name} (★)" : Name;
    }
}