// MediatR Testing Infrastructure - CQRS Testing Foundation
// File: BudgetManagement.Tests/Infrastructure/MediatRTestBase.cs

using BudgetManagement.Shared.Core;
using BudgetManagement.Shared.Infrastructure;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace BudgetManagement.Tests.Infrastructure
{
    /// <summary>
    /// Base class for testing MediatR handlers, commands, and queries
    /// </summary>
    public abstract class MediatRTestBase : AsyncTestBase
    {
        protected readonly Mock<IMediator> MockMediator;
        protected readonly IServiceProvider ServiceProvider;

        protected MediatRTestBase(ITestOutputHelper output) : base(output)
        {
            MockMediator = CreateMock<IMediator>();
            
            // Configure services for MediatR testing
            ConfigureMediatRServices();
            ServiceProvider = BuildServiceProvider();
        }

        /// <summary>
        /// Configures MediatR and related services for testing
        /// </summary>
        protected virtual void ConfigureMediatRServices()
        {
            // Add MediatR
            Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(App).Assembly);
                cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
                cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            });

            // Add FluentValidation
            Services.AddValidatorsFromAssembly(typeof(App).Assembly);

            // Add logging
            Services.AddSingleton(CreateLoggerMock<LoggingBehavior<object, object>>().Object);
            Services.AddSingleton(CreateLoggerMock<ValidationBehavior<object, object>>().Object);
        }

        /// <summary>
        /// Sends a request through MediatR and returns the result
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="request">Request</param>
        /// <returns>Response</returns>
        protected async Task<TResponse> Send<TResponse>(IRequest<TResponse> request)
        {
            var mediator = ServiceProvider.GetRequiredService<IMediator>();
            return await RunWithTimeout(mediator.Send(request, CancellationToken));
        }

        /// <summary>
        /// Sends a command and returns the result
        /// </summary>
        /// <param name="command">Command</param>
        /// <returns>Result</returns>
        protected async Task<Result> Send(ICommand command)
        {
            var mediator = ServiceProvider.GetRequiredService<IMediator>();
            return await RunWithTimeout(mediator.Send(command, CancellationToken));
        }

        /// <summary>
        /// Sends a query and returns the result
        /// </summary>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="query">Query</param>
        /// <returns>Result</returns>
        protected async Task<Result<TResponse>> Send<TResponse>(IQuery<TResponse> query)
        {
            var mediator = ServiceProvider.GetRequiredService<IMediator>();
            return await RunWithTimeout(mediator.Send(query, CancellationToken));
        }

        /// <summary>
        /// Publishes a notification
        /// </summary>
        /// <param name="notification">Notification</param>
        protected async Task Publish(INotification notification)
        {
            var mediator = ServiceProvider.GetRequiredService<IMediator>();
            await RunWithTimeout(mediator.Publish(notification, CancellationToken));
        }

        /// <summary>
        /// Sets up a mock response for a specific request type
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="response">Response to return</param>
        protected void SetupMockResponse<TRequest, TResponse>(TResponse response)
            where TRequest : IRequest<TResponse>
        {
            MockMediator.Setup(m => m.Send(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(response);
        }

        /// <summary>
        /// Verifies that a request was sent through MediatR
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="times">Expected number of times</param>
        protected void VerifyRequestSent<TRequest, TResponse>(Times? times = null)
            where TRequest : IRequest<TResponse>
        {
            MockMediator.Verify(
                m => m.Send(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()),
                times ?? Times.Once);
        }

        /// <summary>
        /// Verifies that a specific request was sent
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="requestMatcher">Request matcher</param>
        /// <param name="times">Expected number of times</param>
        protected void VerifySpecificRequestSent<TRequest, TResponse>(
            Func<TRequest, bool> requestMatcher,
            Times? times = null)
            where TRequest : IRequest<TResponse>
        {
            MockMediator.Verify(
                m => m.Send(It.Is<TRequest>(r => requestMatcher(r)), It.IsAny<CancellationToken>()),
                times ?? Times.Once);
        }
    }

    /// <summary>
    /// Base class for testing command handlers
    /// </summary>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <typeparam name="TCommand">Command type</typeparam>
    public abstract class CommandHandlerTestBase<THandler, TCommand> : MediatRTestBase
        where THandler : class, IRequestHandler<TCommand, Result>
        where TCommand : ICommand
    {
        protected readonly THandler Handler;

        protected CommandHandlerTestBase(ITestOutputHelper output) : base(output)
        {
            Handler = CreateHandler();
        }

        /// <summary>
        /// Creates the handler instance for testing
        /// </summary>
        /// <returns>Handler instance</returns>
        protected abstract THandler CreateHandler();

        /// <summary>
        /// Handles a command using the handler directly
        /// </summary>
        /// <param name="command">Command to handle</param>
        /// <returns>Result</returns>
        protected async Task<Result> Handle(TCommand command)
        {
            return await RunWithTimeout(Handler.Handle(command, CancellationToken));
        }

        /// <summary>
        /// Creates a valid command for testing
        /// </summary>
        /// <returns>Test command</returns>
        protected abstract TCommand CreateValidCommand();

        /// <summary>
        /// Creates an invalid command for testing
        /// </summary>
        /// <returns>Invalid command</returns>
        protected abstract TCommand CreateInvalidCommand();
    }

    /// <summary>
    /// Base class for testing query handlers
    /// </summary>
    /// <typeparam name="THandler">Handler type</typeparam>
    /// <typeparam name="TQuery">Query type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    public abstract class QueryHandlerTestBase<THandler, TQuery, TResponse> : MediatRTestBase
        where THandler : class, IRequestHandler<TQuery, Result<TResponse>>
        where TQuery : IQuery<TResponse>
    {
        protected readonly THandler Handler;

        protected QueryHandlerTestBase(ITestOutputHelper output) : base(output)
        {
            Handler = CreateHandler();
        }

        /// <summary>
        /// Creates the handler instance for testing
        /// </summary>
        /// <returns>Handler instance</returns>
        protected abstract THandler CreateHandler();

        /// <summary>
        /// Handles a query using the handler directly
        /// </summary>
        /// <param name="query">Query to handle</param>
        /// <returns>Result</returns>
        protected async Task<Result<TResponse>> Handle(TQuery query)
        {
            return await RunWithTimeout(Handler.Handle(query, CancellationToken));
        }

        /// <summary>
        /// Creates a valid query for testing
        /// </summary>
        /// <returns>Test query</returns>
        protected abstract TQuery CreateValidQuery();

        /// <summary>
        /// Creates an invalid query for testing
        /// </summary>
        /// <returns>Invalid query</returns>
        protected abstract TQuery CreateInvalidQuery();

        /// <summary>
        /// Asserts that a query result is successful and returns the value
        /// </summary>
        /// <param name="result">Query result</param>
        /// <returns>Result value</returns>
        protected TResponse AssertQuerySuccess(Result<TResponse> result)
        {
            return AssertSuccess(result);
        }

        /// <summary>
        /// Asserts that a query result is a failure
        /// </summary>
        /// <param name="result">Query result</param>
        /// <param name="expectedErrorType">Expected error type</param>
        protected void AssertQueryFailure(Result<TResponse> result, ErrorType? expectedErrorType = null)
        {
            if (expectedErrorType.HasValue)
            {
                AssertFailure(result, expectedErrorType.Value);
            }
            else
            {
                AssertFailure(result);
            }
        }
    }

    /// <summary>
    /// Base class for testing validators
    /// </summary>
    /// <typeparam name="TValidator">Validator type</typeparam>
    /// <typeparam name="TModel">Model type being validated</typeparam>
    public abstract class ValidatorTestBase<TValidator, TModel> : TestBase
        where TValidator : AbstractValidator<TModel>, new()
    {
        protected readonly TValidator Validator;

        protected ValidatorTestBase(ITestOutputHelper output) : base(output)
        {
            Validator = CreateValidator();
        }

        /// <summary>
        /// Creates the validator instance
        /// </summary>
        /// <returns>Validator instance</returns>
        protected virtual TValidator CreateValidator()
        {
            return new TValidator();
        }

        /// <summary>
        /// Validates a model and asserts it's valid
        /// </summary>
        /// <param name="model">Model to validate</param>
        protected void AssertValid(TModel model)
        {
            var result = Validator.Validate(model);
            result.IsValid.Should().BeTrue($"Expected valid model but got errors: {string.Join(", ", result.Errors.Select(e => e.ErrorMessage))}");
        }

        /// <summary>
        /// Validates a model and asserts it's invalid
        /// </summary>
        /// <param name="model">Model to validate</param>
        /// <param name="expectedErrorCount">Expected number of errors</param>
        protected void AssertInvalid(TModel model, int? expectedErrorCount = null)
        {
            var result = Validator.Validate(model);
            result.IsValid.Should().BeFalse("Expected invalid model but validation passed");
            
            if (expectedErrorCount.HasValue)
            {
                result.Errors.Should().HaveCount(expectedErrorCount.Value,
                    $"Expected {expectedErrorCount} errors but got {result.Errors.Count}: {string.Join(", ", result.Errors.Select(e => e.ErrorMessage))}");
            }
        }

        /// <summary>
        /// Validates a model and asserts it has a specific error
        /// </summary>
        /// <param name="model">Model to validate</param>
        /// <param name="propertyName">Property name with error</param>
        /// <param name="errorMessage">Expected error message (optional)</param>
        protected void AssertHasError(TModel model, string propertyName, string? errorMessage = null)
        {
            var result = Validator.Validate(model);
            result.IsValid.Should().BeFalse("Expected validation errors but model was valid");
            
            var propertyErrors = result.Errors.Where(e => e.PropertyName == propertyName).ToList();
            propertyErrors.Should().NotBeEmpty($"Expected error for property '{propertyName}' but none found");
            
            if (!string.IsNullOrEmpty(errorMessage))
            {
                propertyErrors.Should().Contain(e => e.ErrorMessage.Contains(errorMessage),
                    $"Expected error message containing '{errorMessage}' for property '{propertyName}'");
            }
        }

        /// <summary>
        /// Creates a valid model for testing
        /// </summary>
        /// <returns>Valid model</returns>
        protected abstract TModel CreateValidModel();

        /// <summary>
        /// Creates an invalid model for testing
        /// </summary>
        /// <returns>Invalid model</returns>
        protected abstract TModel CreateInvalidModel();
    }

    /// <summary>
    /// Test utilities for MediatR testing
    /// </summary>
    public static class MediatRTestUtilities
    {
        /// <summary>
        /// Creates a mock request handler
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="response">Response to return</param>
        /// <returns>Mock handler</returns>
        public static Mock<IRequestHandler<TRequest, TResponse>> CreateMockHandler<TRequest, TResponse>(TResponse response)
            where TRequest : IRequest<TResponse>
        {
            var mock = new Mock<IRequestHandler<TRequest, TResponse>>();
            mock.Setup(h => h.Handle(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            return mock;
        }

        /// <summary>
        /// Creates a mock notification handler
        /// </summary>
        /// <typeparam name="TNotification">Notification type</typeparam>
        /// <returns>Mock handler</returns>
        public static Mock<INotificationHandler<TNotification>> CreateMockNotificationHandler<TNotification>()
            where TNotification : INotification
        {
            var mock = new Mock<INotificationHandler<TNotification>>();
            mock.Setup(h => h.Handle(It.IsAny<TNotification>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            return mock;
        }

        /// <summary>
        /// Verifies that a handler was called with specific parameters
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="mockHandler">Mock handler</param>
        /// <param name="requestMatcher">Request matcher</param>
        /// <param name="times">Expected call count</param>
        public static void VerifyHandlerCalled<TRequest, TResponse>(
            Mock<IRequestHandler<TRequest, TResponse>> mockHandler,
            Func<TRequest, bool> requestMatcher,
            Times? times = null)
            where TRequest : IRequest<TResponse>
        {
            mockHandler.Verify(
                h => h.Handle(It.Is<TRequest>(r => requestMatcher(r)), It.IsAny<CancellationToken>()),
                times ?? Times.Once);
        }
    }
}