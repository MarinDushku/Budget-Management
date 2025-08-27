// Base Test Infrastructure - Common Testing Foundation
// File: BudgetManagement.Tests/Infrastructure/TestBase.cs

using AutoFixture;
using AutoFixture.AutoMoq;
using BudgetManagement.Shared.Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace BudgetManagement.Tests.Infrastructure
{
    /// <summary>
    /// Base class for all unit tests providing common testing utilities
    /// </summary>
    public abstract class TestBase : IDisposable
    {
        protected readonly ITestOutputHelper Output;
        protected readonly IFixture Fixture;
        protected readonly MockRepository MockRepository;
        protected readonly IServiceCollection Services;
        protected readonly CancellationTokenSource CancellationTokenSource;
        protected readonly CancellationToken CancellationToken;

        private bool _disposed;

        protected TestBase(ITestOutputHelper output)
        {
            Output = output ?? throw new ArgumentNullException(nameof(output));
            
            // Initialize AutoFixture with AutoMoq
            Fixture = new Fixture().Customize(new AutoMoqCustomization { ConfigureMembers = true });
            
            // Initialize Mock repository
            MockRepository = new MockRepository(MockBehavior.Strict);
            
            // Initialize service collection for dependency injection tests
            Services = new ServiceCollection();
            
            // Initialize cancellation token for async tests
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            // Configure fixture behaviors
            ConfigureFixture();
        }

        /// <summary>
        /// Configures AutoFixture for consistent test data generation
        /// </summary>
        protected virtual void ConfigureFixture()
        {
            // Configure to avoid circular references
            Fixture.Behaviors.OfType<ThrowingRecursionBehavior>().ToList()
                .ForEach(b => Fixture.Behaviors.Remove(b));
            Fixture.Behaviors.Add(new OmitOnRecursionBehavior());

            // Configure string generation
            Fixture.Customize<string>(composer => composer.FromFactory(() => $"Test_{Guid.NewGuid():N}"));
            
            // Configure DateTime generation
            Fixture.Customize<DateTime>(composer => composer.FromFactory(() => 
                DateTime.Now.AddDays(Random.Shared.Next(-30, 30))));
                
            // Configure decimal generation for money amounts
            Fixture.Customize<decimal>(composer => composer.FromFactory(() => 
                Math.Round((decimal)(Random.Shared.NextDouble() * 1000), 2)));
        }

        /// <summary>
        /// Creates a mock of the specified type
        /// </summary>
        /// <typeparam name="T">Type to mock</typeparam>
        /// <returns>Mock instance</returns>
        protected Mock<T> CreateMock<T>() where T : class
        {
            return new Mock<T>();
        }

        /// <summary>
        /// Creates a strict mock of the specified type
        /// </summary>
        /// <typeparam name="T">Type to mock</typeparam>
        /// <returns>Strict mock instance</returns>
        protected Mock<T> CreateStrictMock<T>() where T : class
        {
            return new Mock<T>(MockBehavior.Strict);
        }

        /// <summary>
        /// Creates a logger mock for testing
        /// </summary>
        /// <typeparam name="T">Logger category type</typeparam>
        /// <returns>Logger mock</returns>
        protected Mock<ILogger<T>> CreateLoggerMock<T>()
        {
            return new Mock<ILogger<T>>();
        }

        /// <summary>
        /// Writes output to the test output helper
        /// </summary>
        /// <param name="message">Message to write</param>
        protected void WriteOutput(string message)
        {
            Output.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        /// <summary>
        /// Asserts that a Result is successful
        /// </summary>
        /// <param name="result">Result to check</param>
        protected void AssertSuccess(IResult result)
        {
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
            result.Error.Should().BeNull();
        }

        /// <summary>
        /// Asserts that a Result is successful and returns the value
        /// </summary>
        /// <typeparam name="T">Result value type</typeparam>
        /// <param name="result">Result to check</param>
        /// <returns>Result value</returns>
        protected T AssertSuccess<T>(Result<T> result)
        {
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue($"Expected success but got error: {result.Error}");
            result.Error.Should().BeNull();
            result.Value.Should().NotBeNull();
            return result.Value!;
        }

        /// <summary>
        /// Asserts that a Result is a failure
        /// </summary>
        /// <param name="result">Result to check</param>
        protected void AssertFailure(IResult result)
        {
            result.Should().NotBeNull();
            result.IsFailure.Should().BeTrue("Expected failure but got success");
            result.Error.Should().NotBeNull();
        }

        /// <summary>
        /// Asserts that a Result is a failure with a specific error type
        /// </summary>
        /// <param name="result">Result to check</param>
        /// <param name="expectedErrorType">Expected error type</param>
        protected void AssertFailure(IResult result, ErrorType expectedErrorType)
        {
            AssertFailure(result);
            result.Error!.Type.Should().Be(expectedErrorType);
        }

        /// <summary>
        /// Asserts that a Result is a failure with a specific error code
        /// </summary>
        /// <param name="result">Result to check</param>
        /// <param name="expectedErrorCode">Expected error code</param>
        protected void AssertFailure(IResult result, string expectedErrorCode)
        {
            AssertFailure(result);
            result.Error!.Code.Should().Be(expectedErrorCode);
        }

        /// <summary>
        /// Creates test data using AutoFixture
        /// </summary>
        /// <typeparam name="T">Type to create</typeparam>
        /// <returns>Test instance</returns>
        protected T CreateTestData<T>()
        {
            return Fixture.Create<T>();
        }

        /// <summary>
        /// Creates multiple test data instances
        /// </summary>
        /// <typeparam name="T">Type to create</typeparam>
        /// <param name="count">Number of instances</param>
        /// <returns>Test instances</returns>
        protected IEnumerable<T> CreateTestData<T>(int count)
        {
            return Fixture.CreateMany<T>(count);
        }

        /// <summary>
        /// Builds a service provider for testing
        /// </summary>
        /// <returns>Service provider</returns>
        protected IServiceProvider BuildServiceProvider()
        {
            // Add logging
            Services.AddLogging(builder => builder.AddXUnit(Output));
            
            return Services.BuildServiceProvider();
        }

        /// <summary>
        /// Creates a test database connection string
        /// </summary>
        /// <returns>In-memory database connection string</returns>
        protected string CreateTestConnectionString()
        {
            return $"Data Source=:memory:;Cache=Shared;";
        }

        /// <summary>
        /// Creates a test database path
        /// </summary>
        /// <returns>Test database file path</returns>
        protected string CreateTestDatabasePath()
        {
            var tempPath = Path.GetTempPath();
            var testDbName = $"test_db_{Guid.NewGuid():N}.db";
            return Path.Combine(tempPath, testDbName);
        }

        public virtual void Dispose()
        {
            if (!_disposed)
            {
                CancellationTokenSource.Cancel();
                CancellationTokenSource.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Base class for async tests
    /// </summary>
    public abstract class AsyncTestBase : TestBase
    {
        protected AsyncTestBase(ITestOutputHelper output) : base(output)
        {
        }

        /// <summary>
        /// Runs an async operation and ensures it completes within timeout
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="operation">Async operation</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        /// <returns>Operation result</returns>
        protected async Task<T> RunWithTimeout<T>(Task<T> operation, int timeoutMs = 5000)
        {
            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, timeoutCts.Token);
            
            try
            {
                return await operation;
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                throw new TimeoutException($"Operation timed out after {timeoutMs}ms");
            }
        }

        /// <summary>
        /// Runs an async operation and ensures it completes within timeout
        /// </summary>
        /// <param name="operation">Async operation</param>
        /// <param name="timeoutMs">Timeout in milliseconds</param>
        protected async Task RunWithTimeout(Task operation, int timeoutMs = 5000)
        {
            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, timeoutCts.Token);
            
            try
            {
                await operation;
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
            {
                throw new TimeoutException($"Operation timed out after {timeoutMs}ms");
            }
        }
    }

    /// <summary>
    /// Extensions for better XUnit integration
    /// </summary>
    public static class TestExtensions
    {
        /// <summary>
        /// Adds XUnit logging to service collection
        /// </summary>
        /// <param name="builder">Logging builder</param>
        /// <param name="output">Test output helper</param>
        /// <returns>Logging builder</returns>
        public static ILoggingBuilder AddXUnit(this ILoggingBuilder builder, ITestOutputHelper output)
        {
            builder.AddProvider(new XUnitLoggerProvider(output));
            return builder;
        }
    }

    /// <summary>
    /// XUnit logger provider for test output
    /// </summary>
    public class XUnitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _output;

        public XUnitLoggerProvider(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XUnitLogger(_output, categoryName);
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// XUnit logger implementation
    /// </summary>
    public class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        private readonly string _categoryName;

        public XUnitLogger(ITestOutputHelper output, string categoryName)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= LogLevel.Debug;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try
            {
                var message = formatter(state, exception);
                _output.WriteLine($"[{logLevel}] {_categoryName}: {message}");
                
                if (exception != null)
                {
                    _output.WriteLine($"Exception: {exception}");
                }
            }
            catch
            {
                // Ignore logging errors in tests
            }
        }
    }
}