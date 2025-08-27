// Test Data Builders - Consistent Test Data Generation
// File: BudgetManagement.Tests/Infrastructure/TestDataBuilders.cs

using Bogus;
using BudgetManagement.Features.Dashboard.Queries;
using BudgetManagement.Models;
using BudgetManagement.Shared.Core;

namespace BudgetManagement.Tests.Infrastructure
{
    /// <summary>
    /// Test data builders using Bogus library for realistic test data
    /// </summary>
    public static class TestDataBuilders
    {
        private static readonly Faker _faker = new Faker();

        /// <summary>
        /// Builder for Income test data
        /// </summary>
        public class IncomeBuilder
        {
            private readonly Faker<Income> _faker;

            public IncomeBuilder()
            {
                _faker = new Faker<Income>()
                    .RuleFor(i => i.Id, f => f.Random.Int(1, 10000))
                    .RuleFor(i => i.Date, f => f.Date.Between(DateTime.Now.AddYears(-1), DateTime.Now))
                    .RuleFor(i => i.Amount, f => Math.Round(f.Random.Decimal(50, 5000), 2))
                    .RuleFor(i => i.Description, f => f.Finance.TransactionType() + " " + f.Company.CompanyName())
                    .RuleFor(i => i.CreatedAt, f => f.Date.Recent(30))
                    .RuleFor(i => i.UpdatedAt, (f, i) => i.CreatedAt.AddHours(f.Random.Int(0, 24)));
            }

            public IncomeBuilder WithId(int id)
            {
                _faker.RuleFor(i => i.Id, id);
                return this;
            }

            public IncomeBuilder WithDate(DateTime date)
            {
                _faker.RuleFor(i => i.Date, date);
                return this;
            }

            public IncomeBuilder WithAmount(decimal amount)
            {
                _faker.RuleFor(i => i.Amount, amount);
                return this;
            }

            public IncomeBuilder WithDescription(string description)
            {
                _faker.RuleFor(i => i.Description, description);
                return this;
            }

            public IncomeBuilder InDateRange(DateTime startDate, DateTime endDate)
            {
                _faker.RuleFor(i => i.Date, f => f.Date.Between(startDate, endDate));
                return this;
            }

            public Income Build() => _faker.Generate();
            public List<Income> Build(int count) => _faker.Generate(count);
        }

        /// <summary>
        /// Builder for Spending test data
        /// </summary>
        public class SpendingBuilder
        {
            private readonly Faker<Spending> _faker;

            public SpendingBuilder()
            {
                _faker = new Faker<Spending>()
                    .RuleFor(s => s.Id, f => f.Random.Int(1, 10000))
                    .RuleFor(s => s.Date, f => f.Date.Between(DateTime.Now.AddYears(-1), DateTime.Now))
                    .RuleFor(s => s.Amount, f => Math.Round(f.Random.Decimal(5, 500), 2))
                    .RuleFor(s => s.Description, f => f.Commerce.ProductName() + " at " + f.Company.CompanyName())
                    .RuleFor(s => s.CategoryId, f => f.Random.Int(1, 20))
                    .RuleFor(s => s.CreatedAt, f => f.Date.Recent(30))
                    .RuleFor(s => s.UpdatedAt, (f, s) => s.CreatedAt.AddHours(f.Random.Int(0, 24)));
            }

            public SpendingBuilder WithId(int id)
            {
                _faker.RuleFor(s => s.Id, id);
                return this;
            }

            public SpendingBuilder WithDate(DateTime date)
            {
                _faker.RuleFor(s => s.Date, date);
                return this;
            }

            public SpendingBuilder WithAmount(decimal amount)
            {
                _faker.RuleFor(s => s.Amount, amount);
                return this;
            }

            public SpendingBuilder WithDescription(string description)
            {
                _faker.RuleFor(s => s.Description, description);
                return this;
            }

            public SpendingBuilder WithCategoryId(int categoryId)
            {
                _faker.RuleFor(s => s.CategoryId, categoryId);
                return this;
            }

            public SpendingBuilder InDateRange(DateTime startDate, DateTime endDate)
            {
                _faker.RuleFor(s => s.Date, f => f.Date.Between(startDate, endDate));
                return this;
            }

            public Spending Build() => _faker.Generate();
            public List<Spending> Build(int count) => _faker.Generate(count);
        }

        /// <summary>
        /// Builder for Category test data
        /// </summary>
        public class CategoryBuilder
        {
            private readonly Faker<Category> _faker;

            public CategoryBuilder()
            {
                _faker = new Faker<Category>()
                    .RuleFor(c => c.Id, f => f.Random.Int(1, 100))
                    .RuleFor(c => c.Name, f => f.Commerce.Department())
                    .RuleFor(c => c.Icon, f => f.Internet.Emoji())
                    .RuleFor(c => c.IsActive, f => f.Random.Bool(0.9f)); // 90% active
            }

            public CategoryBuilder WithId(int id)
            {
                _faker.RuleFor(c => c.Id, id);
                return this;
            }

            public CategoryBuilder WithName(string name)
            {
                _faker.RuleFor(c => c.Name, name);
                return this;
            }

            public CategoryBuilder WithIcon(string icon)
            {
                _faker.RuleFor(c => c.Icon, icon);
                return this;
            }

            public CategoryBuilder Active(bool isActive = true)
            {
                _faker.RuleFor(c => c.IsActive, isActive);
                return this;
            }

            public Category Build() => _faker.Generate();
            public List<Category> Build(int count) => _faker.Generate(count);
        }

        /// <summary>
        /// Builder for SpendingWithCategory test data
        /// </summary>
        public class SpendingWithCategoryBuilder
        {
            private readonly Faker<SpendingWithCategory> _faker;

            public SpendingWithCategoryBuilder()
            {
                _faker = new Faker<SpendingWithCategory>()
                    .RuleFor(s => s.Id, f => f.Random.Int(1, 10000))
                    .RuleFor(s => s.Date, f => f.Date.Between(DateTime.Now.AddYears(-1), DateTime.Now))
                    .RuleFor(s => s.Amount, f => Math.Round(f.Random.Decimal(5, 500), 2))
                    .RuleFor(s => s.Description, f => f.Commerce.ProductName())
                    .RuleFor(s => s.CategoryId, f => f.Random.Int(1, 20))
                    .RuleFor(s => s.CategoryName, f => f.Commerce.Department())
                    .RuleFor(s => s.CategoryIcon, f => f.Internet.Emoji())
                    .RuleFor(s => s.CreatedAt, f => f.Date.Recent(30))
                    .RuleFor(s => s.UpdatedAt, (f, s) => s.CreatedAt.AddHours(f.Random.Int(0, 24)));
            }

            public SpendingWithCategory Build() => _faker.Generate();
            public List<SpendingWithCategory> Build(int count) => _faker.Generate(count);
        }

        /// <summary>
        /// Builder for BudgetSummary test data
        /// </summary>
        public class BudgetSummaryBuilder
        {
            private readonly BudgetSummary _budgetSummary;

            public BudgetSummaryBuilder()
            {
                _budgetSummary = new BudgetSummary
                {
                    TotalIncome = _faker.Random.Decimal(1000, 10000),
                    TotalSpending = _faker.Random.Decimal(500, 8000),
                    IncomeEntries = _faker.Random.Int(5, 50),
                    SpendingEntries = _faker.Random.Int(10, 100)
                };
            }

            public BudgetSummaryBuilder WithTotalIncome(decimal income)
            {
                _budgetSummary.TotalIncome = income;
                return this;
            }

            public BudgetSummaryBuilder WithTotalSpending(decimal spending)
            {
                _budgetSummary.TotalSpending = spending;
                return this;
            }

            public BudgetSummaryBuilder WithIncomeEntries(int count)
            {
                _budgetSummary.IncomeEntries = count;
                return this;
            }

            public BudgetSummaryBuilder WithSpendingEntries(int count)
            {
                _budgetSummary.SpendingEntries = count;
                return this;
            }

            public BudgetSummary Build() => _budgetSummary;
        }

        /// <summary>
        /// Builder for GetDashboardSummaryQuery test data
        /// </summary>
        public class DashboardQueryBuilder
        {
            private DateTime _startDate;
            private DateTime _endDate;
            private int _bankStatementDay;

            public DashboardQueryBuilder()
            {
                _startDate = DateTime.Now.AddDays(-30);
                _endDate = DateTime.Now;
                _bankStatementDay = 1;
            }

            public DashboardQueryBuilder WithDateRange(DateTime startDate, DateTime endDate)
            {
                _startDate = startDate;
                _endDate = endDate;
                return this;
            }

            public DashboardQueryBuilder WithCurrentMonth()
            {
                var now = DateTime.Now;
                _startDate = new DateTime(now.Year, now.Month, 1);
                _endDate = _startDate.AddMonths(1).AddDays(-1);
                return this;
            }

            public DashboardQueryBuilder WithBankStatementDay(int day)
            {
                _bankStatementDay = day;
                return this;
            }

            public GetDashboardSummaryQuery Build()
            {
                return new GetDashboardSummaryQuery(_startDate, _endDate, _bankStatementDay);
            }
        }

        /// <summary>
        /// Factory for creating related test data sets
        /// </summary>
        public class TestDataSet
        {
            public List<Category> Categories { get; set; } = new();
            public List<Income> Incomes { get; set; } = new();
            public List<Spending> Spendings { get; set; } = new();
            public List<SpendingWithCategory> SpendingsWithCategory { get; set; } = new();

            public static TestDataSet CreateDefault()
            {
                var categories = Category().Build(10);
                var incomes = Income().Build(20);
                var spendings = Spending().Build(50);
                
                // Assign valid category IDs to spending
                foreach (var spending in spendings)
                {
                    spending.CategoryId = _faker.PickRandom(categories).Id;
                }

                var spendingsWithCategory = spendings.Select(s =>
                {
                    var category = categories.First(c => c.Id == s.CategoryId);
                    return new SpendingWithCategory
                    {
                        Id = s.Id,
                        Date = s.Date,
                        Amount = s.Amount,
                        Description = s.Description,
                        CategoryId = s.CategoryId,
                        CategoryName = category.Name,
                        CategoryIcon = category.Icon,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    };
                }).ToList();

                return new TestDataSet
                {
                    Categories = categories,
                    Incomes = incomes,
                    Spendings = spendings,
                    SpendingsWithCategory = spendingsWithCategory
                };
            }

            public static TestDataSet CreateForDateRange(DateTime startDate, DateTime endDate)
            {
                var categories = Category().Build(10);
                var incomes = Income().InDateRange(startDate, endDate).Build(20);
                var spendings = Spending().InDateRange(startDate, endDate).Build(50);
                
                // Assign valid category IDs to spending
                foreach (var spending in spendings)
                {
                    spending.CategoryId = _faker.PickRandom(categories).Id;
                }

                var spendingsWithCategory = spendings.Select(s =>
                {
                    var category = categories.First(c => c.Id == s.CategoryId);
                    return new SpendingWithCategory
                    {
                        Id = s.Id,
                        Date = s.Date,
                        Amount = s.Amount,
                        Description = s.Description,
                        CategoryId = s.CategoryId,
                        CategoryName = category.Name,
                        CategoryIcon = category.Icon,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    };
                }).ToList();

                return new TestDataSet
                {
                    Categories = categories,
                    Incomes = incomes,
                    Spendings = spendings,
                    SpendingsWithCategory = spendingsWithCategory
                };
            }
        }

        // Static factory methods for easy access
        public static IncomeBuilder Income() => new();
        public static SpendingBuilder Spending() => new();
        public static CategoryBuilder Category() => new();
        public static SpendingWithCategoryBuilder SpendingWithCategory() => new();
        public static BudgetSummaryBuilder BudgetSummary() => new();
        public static DashboardQueryBuilder DashboardQuery() => new();
    }

    /// <summary>
    /// Extensions for test data manipulation
    /// </summary>
    public static class TestDataExtensions
    {
        /// <summary>
        /// Converts a list to an async enumerable for testing
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="items">Items</param>
        /// <returns>Async enumerable</returns>
        public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                await Task.Yield(); // Simulate async behavior
                yield return item;
            }
        }

        /// <summary>
        /// Creates a successful Result for testing
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="value">Value</param>
        /// <returns>Successful result</returns>
        public static Result<T> ToSuccessResult<T>(this T value)
        {
            return Result<T>.Success(value);
        }

        /// <summary>
        /// Creates a failed Result for testing
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="error">Error</param>
        /// <returns>Failed result</returns>
        public static Result<T> ToFailureResult<T>(Error error)
        {
            return Result<T>.Failure(error);
        }

        /// <summary>
        /// Creates a test error
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="code">Error code</param>
        /// <param name="type">Error type</param>
        /// <returns>Test error</returns>
        public static Error CreateTestError(
            string message = "Test error", 
            string code = "TEST_ERROR", 
            ErrorType type = ErrorType.System)
        {
            return Error.Create(type, code, message);
        }
    }
}