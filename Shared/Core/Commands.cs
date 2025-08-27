// CQRS Command Infrastructure - Modern Command Pattern Implementation
// File: Shared/Core/Commands.cs

using MediatR;

namespace BudgetManagement.Shared.Core
{
    /// <summary>
    /// Base interface for all commands (operations that modify state)
    /// Commands represent user intent to change the system state
    /// </summary>
    public interface ICommand : IRequest<Result>
    {
        /// <summary>
        /// Unique identifier for tracking command execution
        /// </summary>
        Guid CommandId { get; }

        /// <summary>
        /// Timestamp when the command was created
        /// </summary>
        DateTime CreatedAt { get; }
    }

    /// <summary>
    /// Base interface for commands that return a result value
    /// </summary>
    /// <typeparam name="TResult">Type of the result value</typeparam>
    public interface ICommand<TResult> : IRequest<Result<TResult>>
    {
        /// <summary>
        /// Unique identifier for tracking command execution
        /// </summary>
        Guid CommandId { get; }

        /// <summary>
        /// Timestamp when the command was created
        /// </summary>
        DateTime CreatedAt { get; }
    }

    /// <summary>
    /// Base abstract class for commands providing common properties
    /// </summary>
    public abstract record BaseCommand : ICommand
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Base abstract class for commands with result value
    /// </summary>
    /// <typeparam name="TResult">Type of the result value</typeparam>
    public abstract record BaseCommand<TResult> : ICommand<TResult>
    {
        public Guid CommandId { get; } = Guid.NewGuid();
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Base interface for command handlers
    /// </summary>
    /// <typeparam name="TCommand">Type of command being handled</typeparam>
    public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
        where TCommand : ICommand
    {
    }

    /// <summary>
    /// Base interface for command handlers that return a value
    /// </summary>
    /// <typeparam name="TCommand">Type of command being handled</typeparam>
    /// <typeparam name="TResult">Type of the result value</typeparam>
    public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, Result<TResult>>
        where TCommand : ICommand<TResult>
    {
    }

    /// <summary>
    /// Abstract base class for command handlers providing common functionality
    /// </summary>
    /// <typeparam name="TCommand">Type of command being handled</typeparam>
    public abstract class BaseCommandHandler<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        /// <summary>
        /// Handles the command execution
        /// </summary>
        /// <param name="request">The command to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result of the operation</returns>
        public abstract Task<Result> Handle(TCommand request, CancellationToken cancellationToken);

        /// <summary>
        /// Validates the command before execution
        /// Override to provide custom validation logic
        /// </summary>
        /// <param name="command">Command to validate</param>
        /// <returns>Validation result</returns>
        protected virtual Result ValidateCommand(TCommand command)
        {
            if (command == null)
                return Result.Failure(Error.Validation(Error.Codes.VALIDATION_FAILED, "Command cannot be null"));

            return Result.Success();
        }

        /// <summary>
        /// Logs command execution start
        /// </summary>
        /// <param name="command">Command being executed</param>
        protected virtual void LogCommandStart(TCommand command)
        {
            // TODO: Implement logging when Serilog is configured
            System.Diagnostics.Debug.WriteLine($"Executing command: {command.GetType().Name} [{command.CommandId}]");
        }

        /// <summary>
        /// Logs command execution completion
        /// </summary>
        /// <param name="command">Command that was executed</param>
        /// <param name="result">Result of the execution</param>
        protected virtual void LogCommandComplete(TCommand command, Result result)
        {
            var status = result.IsSuccess ? "SUCCESS" : "FAILURE";
            System.Diagnostics.Debug.WriteLine($"Command completed: {command.GetType().Name} [{command.CommandId}] - {status}");
            
            if (result.IsFailure && result.Error != null)
            {
                System.Diagnostics.Debug.WriteLine($"Command error: {result.Error}");
            }
        }
    }

    /// <summary>
    /// Abstract base class for command handlers that return a value
    /// </summary>
    /// <typeparam name="TCommand">Type of command being handled</typeparam>
    /// <typeparam name="TResult">Type of the result value</typeparam>
    public abstract class BaseCommandHandler<TCommand, TResult> : ICommandHandler<TCommand, TResult>
        where TCommand : ICommand<TResult>
    {
        /// <summary>
        /// Handles the command execution
        /// </summary>
        /// <param name="request">The command to handle</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Result with value of the operation</returns>
        public abstract Task<Result<TResult>> Handle(TCommand request, CancellationToken cancellationToken);

        /// <summary>
        /// Validates the command before execution
        /// Override to provide custom validation logic
        /// </summary>
        /// <param name="command">Command to validate</param>
        /// <returns>Validation result</returns>
        protected virtual Result ValidateCommand(TCommand command)
        {
            if (command == null)
                return Result.Failure(Error.Validation(Error.Codes.VALIDATION_FAILED, "Command cannot be null"));

            return Result.Success();
        }

        /// <summary>
        /// Logs command execution start
        /// </summary>
        /// <param name="command">Command being executed</param>
        protected virtual void LogCommandStart(TCommand command)
        {
            System.Diagnostics.Debug.WriteLine($"Executing command: {command.GetType().Name} [{command.CommandId}]");
        }

        /// <summary>
        /// Logs command execution completion
        /// </summary>
        /// <param name="command">Command that was executed</param>
        /// <param name="result">Result of the execution</param>
        protected virtual void LogCommandComplete(TCommand command, IResult result)
        {
            var status = result.IsSuccess ? "SUCCESS" : "FAILURE";
            System.Diagnostics.Debug.WriteLine($"Command completed: {command.GetType().Name} [{command.CommandId}] - {status}");
            
            if (result.IsFailure && result.Error != null)
            {
                System.Diagnostics.Debug.WriteLine($"Command error: {result.Error}");
            }
        }
    }
}