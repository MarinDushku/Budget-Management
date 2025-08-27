// Dashboard Query Validator Tests - FluentValidation Testing Example
// File: BudgetManagement.Tests/Features/Dashboard/GetDashboardSummaryQueryValidatorTests.cs

using BudgetManagement.Features.Dashboard.Queries;
using BudgetManagement.Features.Dashboard.Validators;
using BudgetManagement.Tests.Infrastructure;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace BudgetManagement.Tests.Features.Dashboard
{
    /// <summary>
    /// Tests for GetDashboardSummaryQueryValidator demonstrating FluentValidation testing
    /// </summary>
    public class GetDashboardSummaryQueryValidatorTests : ValidatorTestBase<GetDashboardSummaryQueryValidator, GetDashboardSummaryQuery>
    {
        public GetDashboardSummaryQueryValidatorTests(ITestOutputHelper output) : base(output)
        {
        }

        protected override GetDashboardSummaryQuery CreateValidModel()
        {
            return TestDataBuilders.DashboardQuery()
                .WithCurrentMonth()
                .WithBankStatementDay(15)
                .Build();
        }

        protected override GetDashboardSummaryQuery CreateInvalidModel()
        {
            // Create query with invalid date range (end before start)
            return TestDataBuilders.DashboardQuery()
                .WithDateRange(DateTime.Now, DateTime.Now.AddDays(-10))
                .Build();
        }

        [Fact]
        public void Validate_WithValidQuery_ShouldPass()
        {
            // Arrange
            var query = CreateValidModel();

            // Act & Assert
            AssertValid(query);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(15)]
        [InlineData(31)]
        public void Validate_WithValidBankStatementDay_ShouldPass(int bankStatementDay)
        {
            // Arrange
            var query = TestDataBuilders.DashboardQuery()
                .WithCurrentMonth()
                .WithBankStatementDay(bankStatementDay)
                .Build();

            // Act & Assert
            AssertValid(query);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(32)]
        [InlineData(100)]
        public void Validate_WithInvalidBankStatementDay_ShouldFail(int bankStatementDay)
        {
            // Arrange
            var query = TestDataBuilders.DashboardQuery()
                .WithCurrentMonth()
                .WithBankStatementDay(bankStatementDay)
                .Build();

            // Act & Assert
            AssertHasError(query, nameof(query.BankStatementDay), "Bank statement day must be between 1 and 31");
        }

        [Fact]
        public void Validate_WithEndDateBeforeStartDate_ShouldFail()
        {
            // Arrange
            var startDate = new DateTime(2024, 3, 15);
            var endDate = new DateTime(2024, 3, 10);
            var query = TestDataBuilders.DashboardQuery()
                .WithDateRange(startDate, endDate)
                .Build();

            // Act & Assert
            AssertHasError(query, nameof(query.EndDate), "End date must be greater than or equal to start date");
        }

        [Fact]
        public void Validate_WithFutureStartDate_ShouldFail()
        {
            // Arrange
            var startDate = DateTime.Now.AddDays(5);
            var endDate = DateTime.Now.AddDays(10);
            var query = TestDataBuilders.DashboardQuery()
                .WithDateRange(startDate, endDate)
                .Build();

            // Act & Assert
            AssertHasError(query, nameof(query.StartDate), "Start date cannot be more than 1 day(s) in the future");
        }

        [Fact]
        public void Validate_WithFutureEndDate_ShouldFail()
        {
            // Arrange
            var startDate = DateTime.Now.AddDays(-5);
            var endDate = DateTime.Now.AddDays(5);
            var query = TestDataBuilders.DashboardQuery()
                .WithDateRange(startDate, endDate)
                .Build();

            // Act & Assert
            AssertHasError(query, nameof(query.EndDate), "End date cannot be more than 1 day(s) in the future");
        }

        [Fact]
        public void Validate_WithDateRangeExceedingTwoYears_ShouldFail()
        {
            // Arrange
            var startDate = DateTime.Now.AddYears(-3);
            var endDate = DateTime.Now;
            var query = TestDataBuilders.DashboardQuery()
                .WithDateRange(startDate, endDate)
                .Build();

            // Act & Assert
            AssertHasError(query, nameof(query.EndDate), "Date range cannot exceed 730 days");
        }

        [Fact]
        public void Validate_WithVeryOldStartDate_ShouldFail()
        {
            // Arrange
            var startDate = DateTime.Now.AddYears(-10);
            var endDate = DateTime.Now.AddYears(-9);
            var query = TestDataBuilders.DashboardQuery()
                .WithDateRange(startDate, endDate)
                .Build();

            // Act & Assert
            AssertHasError(query, nameof(query.StartDate), "Date range cannot be more than 5 years in the past");
        }

        [Fact]
        public void Validate_WithEmptyStartDate_ShouldFail()
        {
            // Arrange
            var query = new GetDashboardSummaryQuery(
                default(DateTime),
                DateTime.Now,
                15);

            // Act & Assert
            AssertHasError(query, nameof(query.StartDate), "Start date is required");
        }

        [Fact]
        public void Validate_WithEmptyEndDate_ShouldFail()
        {
            // Arrange
            var query = new GetDashboardSummaryQuery(
                DateTime.Now.AddDays(-30),
                default(DateTime),
                15);

            // Act & Assert
            AssertHasError(query, nameof(query.EndDate), "End date is required");
        }

        [Fact]
        public void Validate_WithCurrentDateRange_ShouldPass()
        {
            // Arrange
            var now = DateTime.Now.Date;
            var query = TestDataBuilders.DashboardQuery()
                .WithDateRange(now.AddDays(-30), now)
                .Build();

            // Act & Assert
            AssertValid(query);
        }

        [Fact]
        public void Validate_WithSameDayRange_ShouldPass()
        {
            // Arrange
            var today = DateTime.Now.Date;
            var query = TestDataBuilders.DashboardQuery()
                .WithDateRange(today, today)
                .Build();

            // Act & Assert
            AssertValid(query);
        }

        [Fact]
        public void Validate_WithMaximumAllowedRange_ShouldPass()
        {
            // Arrange
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-730); // Exactly 2 years
            var query = TestDataBuilders.DashboardQuery()
                .WithDateRange(startDate, endDate)
                .Build();

            // Act & Assert
            AssertValid(query);
        }

        [Fact]
        public void Validate_WithMaximumAllowedRangePlus1Day_ShouldFail()
        {
            // Arrange
            var endDate = DateTime.Now.Date;
            var startDate = endDate.AddDays(-731); // 2 years + 1 day
            var query = TestDataBuilders.DashboardQuery()
                .WithDateRange(startDate, endDate)
                .Build();

            // Act & Assert
            AssertHasError(query, nameof(query.EndDate), "Date range cannot exceed 730 days");
        }

        [Fact]
        public void Validate_WithBoundaryBankStatementDays_ShouldPassOrFail()
        {
            // Test boundary values for bank statement day
            var validQuery1 = TestDataBuilders.DashboardQuery()
                .WithCurrentMonth()
                .WithBankStatementDay(1)
                .Build();

            var validQuery31 = TestDataBuilders.DashboardQuery()
                .WithCurrentMonth()
                .WithBankStatementDay(31)
                .Build();

            var invalidQuery0 = TestDataBuilders.DashboardQuery()
                .WithCurrentMonth()
                .WithBankStatementDay(0)
                .Build();

            var invalidQuery32 = TestDataBuilders.DashboardQuery()
                .WithCurrentMonth()
                .WithBankStatementDay(32)
                .Build();

            // Act & Assert
            AssertValid(validQuery1);
            AssertValid(validQuery31);
            AssertInvalid(invalidQuery0);
            AssertInvalid(invalidQuery32);
        }

        [Fact]
        public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
        {
            // Arrange - create query with multiple validation errors
            var query = new GetDashboardSummaryQuery(
                DateTime.Now.AddDays(5),     // Future start date
                DateTime.Now.AddDays(-5),    // End date before start date
                0);                          // Invalid bank statement day

            // Act & Assert
            AssertInvalid(query, expectedErrorCount: 3);
            
            // Verify specific errors
            AssertHasError(query, nameof(query.StartDate));
            AssertHasError(query, nameof(query.EndDate));
            AssertHasError(query, nameof(query.BankStatementDay));
        }
    }
}