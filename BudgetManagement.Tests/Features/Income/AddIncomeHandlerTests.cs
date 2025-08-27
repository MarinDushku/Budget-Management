// Add Income Handler Tests - Unit Testing for Vertical Slice Architecture
// File: BudgetManagement.Tests/Features/Income/AddIncomeHandlerTests.cs

using BudgetManagement.Features.Income.Commands;
using BudgetManagement.Features.Income.Handlers;
using BudgetManagement.Models;
using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Data.Repositories;
using BudgetManagement.Shared.Infrastructure;
using BudgetManagement.Shared.Infrastructure.Caching;
using BudgetManagement.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BudgetManagement.Tests.Features.Income
{
    public class AddIncomeHandlerTests : TestBase
    {
        private readonly IIncomeRepository _incomeRepository;
        private readonly IBudgetCacheService _cacheService;
        private readonly IApplicationLoggingService _loggingService;
        private readonly ILogger<AddIncomeHandler> _logger;
        private readonly AddIncomeHandler _handler;

        public AddIncomeHandlerTests()
        {
            _incomeRepository = CreateMock<IIncomeRepository>();
            _cacheService = CreateMock<IBudgetCacheService>();
            _loggingService = CreateMock<IApplicationLoggingService>();
            _logger = CreateMock<ILogger<AddIncomeHandler>>();
            _handler = new AddIncomeHandler(_incomeRepository, _cacheService, _loggingService, _logger);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldAddIncomeAndReturnSuccess()
        {
            // Arrange
            var command = new AddIncomeCommand(
                Date: DateTime.Today,
                Amount: 1500.50m,
                Description: "Test salary payment");

            var expectedIncome = new Models.Income
            {
                Id = 1,
                Date = command.Date,
                Amount = command.Amount,
                Description = command.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _incomeRepository.AddAsync(Arg.Any<Models.Income>(), Arg.Any<CancellationToken>())
                .Returns(Result<Models.Income>.Success(expectedIncome));

            _cacheService.InvalidateIncomeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success());
            
            _cacheService.InvalidateDashboardSummariesAsync(Arg.Any<CancellationToken>())
                .Returns(Result.Success());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeSuccessful();
            result.Value.Should().NotBeNull();
            result.Value!.Amount.Should().Be(command.Amount);
            result.Value.Description.Should().Be(command.Description);
            result.Value.Date.Should().Be(command.Date);

            // Verify repository was called
            await _incomeRepository.Received(1).AddAsync(
                Arg.Is<Models.Income>(i => 
                    i.Amount == command.Amount &&
                    i.Description == command.Description &&
                    i.Date == command.Date),
                Arg.Any<CancellationToken>());

            // Verify cache invalidation
            await _cacheService.Received(1).InvalidateIncomeAsync(command.Date, Arg.Any<CancellationToken>());
            await _cacheService.Received(1).InvalidateDashboardSummariesAsync(Arg.Any<CancellationToken>());

            // Verify logging
            await _loggingService.Received(1).LogIncomeAddedAsync(
                expectedIncome.Id,
                expectedIncome.Amount,
                expectedIncome.Date,
                expectedIncome.Description,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_RepositoryFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new AddIncomeCommand(
                Date: DateTime.Today,
                Amount: 1000m,
                Description: "Test income");

            var error = Error.System(Error.Codes.DATABASE_ERROR, "Database connection failed");
            _incomeRepository.AddAsync(Arg.Any<Models.Income>(), Arg.Any<CancellationToken>())
                .Returns(Result<Models.Income>.Failure(error));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFailure();
            result.Error.Should().Be(error);

            // Verify cache invalidation was not called
            await _cacheService.DidNotReceive().InvalidateIncomeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
            await _cacheService.DidNotReceive().InvalidateDashboardSummariesAsync(Arg.Any<CancellationToken>());

            // Verify logging was not called
            await _loggingService.DidNotReceive().LogIncomeAddedAsync(
                Arg.Any<int>(), Arg.Any<decimal>(), Arg.Any<DateTime>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_CacheInvalidationFails_ShouldStillReturnSuccess()
        {
            // Arrange
            var command = new AddIncomeCommand(
                Date: DateTime.Today,
                Amount: 2000m,
                Description: "Test bonus payment");

            var expectedIncome = new Models.Income
            {
                Id = 2,
                Date = command.Date,
                Amount = command.Amount,
                Description = command.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _incomeRepository.AddAsync(Arg.Any<Models.Income>(), Arg.Any<CancellationToken>())
                .Returns(Result<Models.Income>.Success(expectedIncome));

            // Cache invalidation fails, but operation should still succeed
            _cacheService.InvalidateIncomeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(Result.Failure(Error.System(Error.Codes.CACHE_ERROR, "Cache service unavailable")));
            
            _cacheService.InvalidateDashboardSummariesAsync(Arg.Any<CancellationToken>())
                .Returns(Result.Success());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeSuccessful();
            result.Value.Should().NotBeNull();
            result.Value!.Id.Should().Be(expectedIncome.Id);

            // Verify repository was called
            await _incomeRepository.Received(1).AddAsync(Arg.Any<Models.Income>(), Arg.Any<CancellationToken>());

            // Verify cache invalidation was attempted
            await _cacheService.Received(1).InvalidateIncomeAsync(command.Date, Arg.Any<CancellationToken>());

            // Verify logging was still called
            await _loggingService.Received(1).LogIncomeAddedAsync(
                expectedIncome.Id,
                expectedIncome.Amount,
                expectedIncome.Date,
                expectedIncome.Description,
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task Handle_ExceptionThrown_ShouldReturnSystemError()
        {
            // Arrange
            var command = new AddIncomeCommand(
                Date: DateTime.Today,
                Amount: 500m,
                Description: "Test income");

            _incomeRepository.AddAsync(Arg.Any<Models.Income>(), Arg.Any<CancellationToken>())
                .Throws(new InvalidOperationException("Unexpected database error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeFailure();
            result.Error.Should().NotBeNull();
            result.Error!.Code.Should().Be(Error.Codes.SYSTEM_ERROR);
            result.Error.Message.Should().Be("Failed to add income entry");
            result.Error.Metadata.Should().ContainKeys("Amount", "Date", "Description");
        }

        [Theory]
        [InlineData(0.01, "Minimum amount")]
        [InlineData(999999.99, "Maximum amount")]
        [InlineData(1234.56, "Regular amount with cents")]
        public async Task Handle_ValidAmounts_ShouldSucceed(decimal amount, string description)
        {
            // Arrange
            var command = new AddIncomeCommand(
                Date: DateTime.Today,
                Amount: amount,
                Description: description);

            var expectedIncome = new Models.Income
            {
                Id = 3,
                Date = command.Date,
                Amount = command.Amount,
                Description = command.Description,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _incomeRepository.AddAsync(Arg.Any<Models.Income>(), Arg.Any<CancellationToken>())
                .Returns(Result<Models.Income>.Success(expectedIncome));

            _cacheService.InvalidateIncomeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success());
            
            _cacheService.InvalidateDashboardSummariesAsync(Arg.Any<CancellationToken>())
                .Returns(Result.Success());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeSuccessful();
            result.Value!.Amount.Should().Be(amount);
            result.Value.Description.Should().Be(description);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldSetTimestamps()
        {
            // Arrange
            var command = new AddIncomeCommand(
                Date: DateTime.Today,
                Amount: 1000m,
                Description: "Timestamp test");

            var capturedIncome = default(Models.Income);
            _incomeRepository.AddAsync(Arg.Do<Models.Income>(i => capturedIncome = i), Arg.Any<CancellationToken>())
                .Returns(callInfo => 
                {
                    var income = callInfo.Arg<Models.Income>();
                    income.Id = 4; // Simulate database ID assignment
                    return Result<Models.Income>.Success(income);
                });

            _cacheService.InvalidateIncomeAsync(Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success());
            
            _cacheService.InvalidateDashboardSummariesAsync(Arg.Any<CancellationToken>())
                .Returns(Result.Success());

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeSuccessful();
            capturedIncome.Should().NotBeNull();
            capturedIncome!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            capturedIncome.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            capturedIncome.CreatedAt.Should().Be(capturedIncome.UpdatedAt);
        }
    }
}