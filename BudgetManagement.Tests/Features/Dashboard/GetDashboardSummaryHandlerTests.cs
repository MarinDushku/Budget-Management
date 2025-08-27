// Dashboard Summary Handler Tests - Example Test Implementation
// File: BudgetManagement.Tests/Features/Dashboard/GetDashboardSummaryHandlerTests.cs

using BudgetManagement.Features.Dashboard.Handlers;
using BudgetManagement.Features.Dashboard.Queries;
using BudgetManagement.Models;
using BudgetManagement.Services;
using BudgetManagement.Shared.Core;
using BudgetManagement.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BudgetManagement.Tests.Features.Dashboard
{
    /// <summary>
    /// Tests for GetDashboardSummaryHandler demonstrating the testing infrastructure
    /// </summary>
    public class GetDashboardSummaryHandlerTests : QueryHandlerTestBase<GetDashboardSummaryHandler, GetDashboardSummaryQuery, DashboardSummary>
    {
        private readonly Mock<IBudgetService> _mockBudgetService;
        private readonly Mock<ILogger<GetDashboardSummaryHandler>> _mockLogger;

        public GetDashboardSummaryHandlerTests(ITestOutputHelper output) : base(output)
        {
            _mockBudgetService = CreateMock<IBudgetService>();
            _mockLogger = CreateLoggerMock<GetDashboardSummaryHandler>();
        }

        protected override GetDashboardSummaryHandler CreateHandler()
        {
            return new GetDashboardSummaryHandler(_mockBudgetService.Object, _mockLogger.Object);
        }

        protected override GetDashboardSummaryQuery CreateValidQuery()
        {
            return TestDataBuilders.DashboardQuery()
                .WithCurrentMonth()
                .Build();
        }

        protected override GetDashboardSummaryQuery CreateInvalidQuery()
        {
            // Create query with end date before start date
            var startDate = DateTime.Now;
            var endDate = DateTime.Now.AddDays(-10);
            return TestDataBuilders.DashboardQuery()
                .WithDateRange(startDate, endDate)
                .Build();
        }

        [Fact]
        public async Task Handle_WithValidQuery_ShouldReturnDashboardSummary()
        {
            // Arrange
            var query = CreateValidQuery();
            var testData = TestDataBuilders.TestDataSet.CreateForDateRange(query.StartDate, query.EndDate);
            
            var expectedBudgetSummary = TestDataBuilders.BudgetSummary()
                .WithTotalIncome(5000m)
                .WithTotalSpending(3500m)
                .Build();

            var expectedBankStatementSummary = CreateTestData<BankStatementSummary>();

            // Setup mock responses
            _mockBudgetService.Setup(s => s.GetBudgetSummaryAsync(query.StartDate, query.EndDate))
                .ReturnsAsync(expectedBudgetSummary);

            _mockBudgetService.Setup(s => s.GetBankStatementSummaryAsync(query.BankStatementDay))
                .ReturnsAsync(expectedBankStatementSummary);

            _mockBudgetService.Setup(s => s.GetIncomeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(testData.Incomes);

            _mockBudgetService.Setup(s => s.GetSpendingWithCategoryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(testData.SpendingsWithCategory);

            _mockBudgetService.Setup(s => s.GetSpendingAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(testData.Spendings);

            // Act
            var result = await Handle(query);

            // Assert
            var dashboardSummary = AssertQuerySuccess(result);
            
            dashboardSummary.Should().NotBeNull();
            dashboardSummary.BudgetSummary.Should().BeEquivalentTo(expectedBudgetSummary);
            dashboardSummary.BankStatementSummary.Should().BeEquivalentTo(expectedBankStatementSummary);
            dashboardSummary.RecentIncomeEntries.Should().NotBeEmpty();
            dashboardSummary.RecentSpendingEntries.Should().NotBeEmpty();

            // Verify all service calls were made
            _mockBudgetService.Verify(s => s.GetBudgetSummaryAsync(query.StartDate, query.EndDate), Times.Once);
            _mockBudgetService.Verify(s => s.GetBankStatementSummaryAsync(query.BankStatementDay), Times.Once);
        }

        [Fact]
        public async Task Handle_WithInvalidDateRange_ShouldReturnValidationError()
        {
            // Arrange
            var query = CreateInvalidQuery();

            // Act
            var result = await Handle(query);

            // Assert
            AssertQueryFailure(result, ErrorType.Validation);
            result.Error!.Code.Should().Be(Error.Codes.INVALID_DATE);
        }

        [Fact]
        public async Task Handle_WhenBudgetServiceFails_ShouldReturnSystemError()
        {
            // Arrange
            var query = CreateValidQuery();
            
            _mockBudgetService.Setup(s => s.GetBudgetSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            // Act
            var result = await Handle(query);

            // Assert
            AssertQueryFailure(result, ErrorType.System);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(32)]
        public async Task Handle_WithInvalidBankStatementDay_ShouldReturnValidationError(int invalidDay)
        {
            // Arrange
            var query = TestDataBuilders.DashboardQuery()
                .WithCurrentMonth()
                .WithBankStatementDay(invalidDay)
                .Build();

            // Act
            var result = await Handle(query);

            // Assert
            AssertQueryFailure(result, ErrorType.Validation);
        }

        [Fact]
        public async Task Handle_WithFutureEndDate_ShouldReturnValidationError()
        {
            // Arrange
            var query = TestDataBuilders.DashboardQuery()
                .WithDateRange(DateTime.Now, DateTime.Now.AddDays(5))
                .Build();

            // Act
            var result = await Handle(query);

            // Assert
            AssertQueryFailure(result, ErrorType.Validation);
        }

        [Fact]
        public async Task Handle_WithLargeDateRange_ShouldReturnValidationError()
        {
            // Arrange
            var startDate = DateTime.Now.AddYears(-3);
            var endDate = DateTime.Now;
            var query = TestDataBuilders.DashboardQuery()
                .WithDateRange(startDate, endDate)
                .Build();

            // Act
            var result = await Handle(query);

            // Assert
            AssertQueryFailure(result, ErrorType.Validation);
        }

        [Fact]
        public async Task Handle_WithEmptyData_ShouldReturnEmptyDashboardSummary()
        {
            // Arrange
            var query = CreateValidQuery();
            var emptyBudgetSummary = TestDataBuilders.BudgetSummary()
                .WithTotalIncome(0m)
                .WithTotalSpending(0m)
                .WithIncomeEntries(0)
                .WithSpendingEntries(0)
                .Build();

            _mockBudgetService.Setup(s => s.GetBudgetSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(emptyBudgetSummary);

            _mockBudgetService.Setup(s => s.GetBankStatementSummaryAsync(It.IsAny<int>()))
                .ReturnsAsync(CreateTestData<BankStatementSummary>());

            _mockBudgetService.Setup(s => s.GetIncomeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Income>());

            _mockBudgetService.Setup(s => s.GetSpendingWithCategoryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<SpendingWithCategory>());

            _mockBudgetService.Setup(s => s.GetSpendingAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(new List<Spending>());

            // Act
            var result = await Handle(query);

            // Assert
            var dashboardSummary = AssertQuerySuccess(result);
            
            dashboardSummary.BudgetSummary.TotalIncome.Should().Be(0m);
            dashboardSummary.BudgetSummary.TotalSpending.Should().Be(0m);
            dashboardSummary.HasNoRecentEntries.Should().BeTrue();
            dashboardSummary.AverageDailySpending.Should().Be(0m);
        }

        [Fact]
        public async Task Handle_ShouldCalculateCorrectAverageDailySpending()
        {
            // Arrange
            var query = CreateValidQuery();
            var daysDifference = (query.EndDate - query.StartDate).Days + 1;
            var totalSpending = 300m;
            var expectedAverage = totalSpending / daysDifference;

            var spendingData = new List<SpendingWithCategory>
            {
                TestDataBuilders.SpendingWithCategory().Build(),
                TestDataBuilders.SpendingWithCategory().Build()
            };
            spendingData[0].Amount = 100m;
            spendingData[1].Amount = 200m;

            SetupMinimalMocks(query, spendingWithCategory: spendingData);

            // Act
            var result = await Handle(query);

            // Assert
            var dashboardSummary = AssertQuerySuccess(result);
            dashboardSummary.AverageDailySpending.Should().BeApproximately(expectedAverage, 0.01m);
        }

        [Fact]
        public async Task Handle_ShouldLimitRecentEntriesToFiveItems()
        {
            // Arrange
            var query = CreateValidQuery();
            var manyIncomes = TestDataBuilders.Income().Build(20);
            var manySpendingsWithCategory = TestDataBuilders.SpendingWithCategory().Build(30);

            SetupMinimalMocks(query, incomes: manyIncomes, spendingWithCategory: manySpendingsWithCategory);

            // Act
            var result = await Handle(query);

            // Assert
            var dashboardSummary = AssertQuerySuccess(result);
            dashboardSummary.RecentIncomeEntries.Should().HaveCount(5);
            dashboardSummary.RecentSpendingEntries.Should().HaveCount(5);
        }

        [Fact]
        public async Task Handle_ShouldFormatPeriodDescriptionCorrectly()
        {
            // Arrange
            var startDate = new DateTime(2024, 3, 1);
            var endDate = new DateTime(2024, 3, 31);
            var query = TestDataBuilders.DashboardQuery()
                .WithDateRange(startDate, endDate)
                .Build();

            SetupMinimalMocks(query);

            // Act
            var result = await Handle(query);

            // Assert
            var dashboardSummary = AssertQuerySuccess(result);
            dashboardSummary.PeriodDescription.Should().Be("March 2024");
        }

        /// <summary>
        /// Sets up minimal mocks for testing basic functionality
        /// </summary>
        private void SetupMinimalMocks(
            GetDashboardSummaryQuery query, 
            IEnumerable<Income>? incomes = null,
            IEnumerable<SpendingWithCategory>? spendingWithCategory = null,
            IEnumerable<Spending>? spending = null)
        {
            _mockBudgetService.Setup(s => s.GetBudgetSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(TestDataBuilders.BudgetSummary().Build());

            _mockBudgetService.Setup(s => s.GetBankStatementSummaryAsync(It.IsAny<int>()))
                .ReturnsAsync(CreateTestData<BankStatementSummary>());

            _mockBudgetService.Setup(s => s.GetIncomeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(incomes ?? TestDataBuilders.Income().Build(5));

            _mockBudgetService.Setup(s => s.GetSpendingWithCategoryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(spendingWithCategory ?? TestDataBuilders.SpendingWithCategory().Build(5));

            _mockBudgetService.Setup(s => s.GetSpendingAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(spending ?? TestDataBuilders.Spending().Build(5));
        }
    }
}