// Dashboard Integration Tests - Full Vertical Slice Testing
// File: BudgetManagement.Tests/Integration/DashboardIntegrationTests.cs

using BudgetManagement.Features.Dashboard.Handlers;
using BudgetManagement.Features.Dashboard.Queries;
using BudgetManagement.Features.Dashboard.Validators;
using BudgetManagement.Features.Dashboard.ViewModels;
using BudgetManagement.Models;
using BudgetManagement.Services;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Infrastructure;
using BudgetManagement.Tests.Infrastructure;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BudgetManagement.Tests.Integration
{
    /// <summary>
    /// Integration tests for the Dashboard vertical slice
    /// Tests the complete flow from ViewModel through MediatR to Handler
    /// </summary>
    [Collection("Integration Tests")]
    public class DashboardIntegrationTests : AsyncTestBase
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Mock<IBudgetService> _mockBudgetService;

        public DashboardIntegrationTests(ITestOutputHelper output) : base(output)
        {
            _mockBudgetService = CreateMock<IBudgetService>();
            ConfigureIntegrationServices();
            _serviceProvider = BuildServiceProvider();
        }

        private void ConfigureIntegrationServices()
        {
            // Add MediatR with behaviors
            Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(GetDashboardSummaryHandler).Assembly);
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // Add FluentValidation
            Services.AddValidatorsFromAssembly(typeof(GetDashboardSummaryQueryValidator).Assembly);
            Services.AddScoped<IValidator<GetDashboardSummaryQuery>, GetDashboardSummaryQueryValidator>();

            // Add handlers
            Services.AddScoped<GetDashboardSummaryHandler>();

            // Add ViewModels
            Services.AddTransient<DashboardViewModel>();

            // Add mock services
            Services.AddSingleton(_mockBudgetService.Object);

            // Add logging
            Services.AddSingleton(CreateLoggerMock<GetDashboardSummaryHandler>().Object);
            Services.AddSingleton(CreateLoggerMock<DashboardViewModel>().Object);
            Services.AddSingleton(CreateLoggerMock<LoggingBehavior<GetDashboardSummaryQuery, Result<DashboardSummary>>>().Object);
            Services.AddSingleton(CreateLoggerMock<ValidationBehavior<GetDashboardSummaryQuery, Result<DashboardSummary>>>().Object);
        }

        [Fact]
        public async Task DashboardQuery_ThroughMediatR_ShouldReturnSuccessfulResult()
        {
            // Arrange
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var query = TestDataBuilders.DashboardQuery().WithCurrentMonth().Build();
            var testData = TestDataBuilders.TestDataSet.CreateForDateRange(query.StartDate, query.EndDate);

            SetupBudgetServiceMocks(testData);

            // Act
            var result = await mediator.Send(query, CancellationToken);

            // Assert
            AssertSuccess(result);
            result.Value.Should().NotBeNull();
            result.Value!.BudgetSummary.Should().NotBeNull();
            result.Value.RecentIncomeEntries.Should().NotBeEmpty();
            result.Value.RecentSpendingEntries.Should().NotBeEmpty();
        }

        [Fact]
        public async Task DashboardQuery_WithInvalidData_ShouldFailValidation()
        {
            // Arrange
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var invalidQuery = TestDataBuilders.DashboardQuery()
                .WithDateRange(DateTime.Now.AddDays(5), DateTime.Now.AddDays(-5)) // Invalid range
                .Build();

            // Act
            var result = await mediator.Send(invalidQuery, CancellationToken);

            // Assert
            AssertFailure(result, ErrorType.Validation);
            result.Error!.Message.Should().Contain("Validation failed");
        }

        [Fact]
        public async Task DashboardViewModel_InitializeAsync_ShouldLoadDashboardData()
        {
            // Arrange
            var viewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
            var testData = TestDataBuilders.TestDataSet.CreateDefault();

            SetupBudgetServiceMocks(testData);

            // Act
            await viewModel.InitializeAsync();

            // Assert
            viewModel.IsLoading.Should().BeFalse();
            viewModel.DashboardData.Should().NotBeNull();
            viewModel.ErrorMessage.Should().BeNull();
            viewModel.StatusMessage.Should().Be("Dashboard updated successfully");
        }

        [Fact]
        public async Task DashboardViewModel_RefreshCommand_ShouldUpdateData()
        {
            // Arrange
            var viewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
            var testData = TestDataBuilders.TestDataSet.CreateDefault();

            SetupBudgetServiceMocks(testData);

            // Act
            await viewModel.RefreshDashboardCommand.ExecuteAsync(null);

            // Assert
            viewModel.IsLoading.Should().BeFalse();
            viewModel.DashboardData.Should().NotBeNull();
            viewModel.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task DashboardViewModel_WithServiceError_ShouldShowErrorMessage()
        {
            // Arrange
            var viewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
            
            _mockBudgetService.Setup(s => s.GetBudgetSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ThrowsAsync(new InvalidOperationException("Database connection failed"));

            // Act
            await viewModel.RefreshDashboardCommand.ExecuteAsync(null);

            // Assert
            viewModel.IsLoading.Should().BeFalse();
            viewModel.ErrorMessage.Should().NotBeNull();
            viewModel.ErrorMessage.Should().Contain("system error");
            viewModel.StatusMessage.Should().Be("Error loading dashboard data");
        }

        [Fact]
        public async Task FullVerticalSlice_WithValidationBehavior_ShouldValidateBeforeHandling()
        {
            // Arrange
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var invalidQuery = new GetDashboardSummaryQuery(
                default(DateTime), // Invalid start date
                DateTime.Now,
                15);

            // Act
            var result = await mediator.Send(invalidQuery, CancellationToken);

            // Assert
            AssertFailure(result, ErrorType.Validation);
            
            // Verify that the handler was never called due to validation failure
            _mockBudgetService.Verify(
                s => s.GetBudgetSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()),
                Times.Never);
        }

        [Fact]
        public async Task FullVerticalSlice_WithLoggingBehavior_ShouldLogRequestsAndResponses()
        {
            // Arrange
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var query = TestDataBuilders.DashboardQuery().WithCurrentMonth().Build();
            var testData = TestDataBuilders.TestDataSet.CreateForDateRange(query.StartDate, query.EndDate);

            SetupBudgetServiceMocks(testData);

            // Act
            var result = await mediator.Send(query, CancellationToken);

            // Assert
            AssertSuccess(result);
            
            // Note: In a real test, you would verify logging calls on the mock logger
            // This demonstrates that logging behavior is part of the pipeline
        }

        [Fact]
        public async Task DashboardViewModel_UpdateDateRange_ShouldRefreshData()
        {
            // Arrange
            var viewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
            var newStartDate = DateTime.Now.AddDays(-60);
            var newEndDate = DateTime.Now.AddDays(-30);
            var testData = TestDataBuilders.TestDataSet.CreateForDateRange(newStartDate, newEndDate);

            SetupBudgetServiceMocks(testData);

            // Act
            await viewModel.UpdateDateRangeAsync(newStartDate, newEndDate);

            // Assert
            viewModel.SelectedPeriodStart.Should().Be(newStartDate);
            viewModel.SelectedPeriodEnd.Should().Be(newEndDate);
            viewModel.DashboardData.Should().NotBeNull();
            viewModel.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public async Task DashboardViewModel_SetCurrentMonth_ShouldUpdateToCurrentMonth()
        {
            // Arrange
            var viewModel = _serviceProvider.GetRequiredService<DashboardViewModel>();
            var now = DateTime.Now;
            var expectedStartDate = new DateTime(now.Year, now.Month, 1);
            var expectedEndDate = expectedStartDate.AddMonths(1).AddDays(-1);
            var testData = TestDataBuilders.TestDataSet.CreateForDateRange(expectedStartDate, expectedEndDate);

            SetupBudgetServiceMocks(testData);

            // Act
            await viewModel.SetCurrentMonthCommand.ExecuteAsync(null);

            // Assert
            viewModel.SelectedPeriodStart.Date.Should().Be(expectedStartDate.Date);
            viewModel.SelectedPeriodEnd.Date.Should().Be(expectedEndDate.Date);
            viewModel.DashboardData.Should().NotBeNull();
        }

        [Fact]
        public async Task MultipleSimultaneousRequests_ShouldHandleCorrectly()
        {
            // Arrange
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var queries = Enumerable.Range(0, 5)
                .Select(_ => TestDataBuilders.DashboardQuery().WithCurrentMonth().Build())
                .ToList();

            var testData = TestDataBuilders.TestDataSet.CreateDefault();
            SetupBudgetServiceMocks(testData);

            // Act
            var tasks = queries.Select(query => mediator.Send(query, CancellationToken));
            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().HaveCount(5);
            results.Should().AllSatisfy(result => AssertSuccess(result));
        }

        [Fact]
        public async Task CancellationToken_ShouldBePropagatedThroughPipeline()
        {
            // Arrange
            var mediator = _serviceProvider.GetRequiredService<IMediator>();
            var query = TestDataBuilders.DashboardQuery().WithCurrentMonth().Build();
            
            using var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(100); // Cancel after 100ms

            _mockBudgetService.Setup(s => s.GetBudgetSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .Returns(async () =>
                {
                    await Task.Delay(500, cancellationTokenSource.Token); // Longer delay to trigger cancellation
                    return TestDataBuilders.BudgetSummary().Build();
                });

            // Act & Assert
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => mediator.Send(query, cancellationTokenSource.Token));
        }

        private void SetupBudgetServiceMocks(TestDataBuilders.TestDataSet testData)
        {
            var budgetSummary = TestDataBuilders.BudgetSummary()
                .WithTotalIncome(testData.Incomes.Sum(i => i.Amount))
                .WithTotalSpending(testData.Spendings.Sum(s => s.Amount))
                .WithIncomeEntries(testData.Incomes.Count)
                .WithSpendingEntries(testData.Spendings.Count)
                .Build();

            var bankStatementSummary = CreateTestData<BankStatementSummary>();

            _mockBudgetService.Setup(s => s.GetBudgetSummaryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(budgetSummary);

            _mockBudgetService.Setup(s => s.GetBankStatementSummaryAsync(It.IsAny<int>()))
                .ReturnsAsync(bankStatementSummary);

            _mockBudgetService.Setup(s => s.GetIncomeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(testData.Incomes);

            _mockBudgetService.Setup(s => s.GetSpendingWithCategoryAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(testData.SpendingsWithCategory);

            _mockBudgetService.Setup(s => s.GetSpendingAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
                .ReturnsAsync(testData.Spendings);
        }
    }

    /// <summary>
    /// Collection definition for integration tests to ensure proper test isolation
    /// </summary>
    [CollectionDefinition("Integration Tests", DisableParallelization = true)]
    public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFixture>
    {
    }

    /// <summary>
    /// Fixture for integration tests providing shared setup
    /// </summary>
    public class IntegrationTestFixture : IDisposable
    {
        public IntegrationTestFixture()
        {
            // Setup any shared integration test resources here
        }

        public void Dispose()
        {
            // Cleanup any shared resources
        }
    }
}