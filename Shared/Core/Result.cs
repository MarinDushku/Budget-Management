// Result Pattern Implementation - Modern Functional Error Handling
// File: Shared/Core/Result.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace BudgetManagement.Shared.Core
{
    /// <summary>
    /// Non-generic result for operations that don't return a value
    /// Represents success or failure without a return value
    /// </summary>
    public class Result : IResult
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Error? Error { get; }
        public IReadOnlyCollection<Error>? Errors { get; }

        protected Result(bool isSuccess, Error? error = null, IReadOnlyCollection<Error>? errors = null)
        {
            IsSuccess = isSuccess;
            Error = error;
            Errors = errors;
        }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static Result Success() => new(true);

        /// <summary>
        /// Creates a failed result with a single error
        /// </summary>
        public static Result Failure(Error error) => new(false, error);

        /// <summary>
        /// Creates a failed result with multiple errors
        /// </summary>
        public static Result Failure(IEnumerable<Error> errors)
        {
            var errorList = errors.ToList();
            return new(false, errorList.FirstOrDefault(), errorList);
        }

        /// <summary>
        /// Creates a failed result from an exception
        /// </summary>
        public static Result Failure(Exception exception)
        {
            var error = Error.System(
                Error.Codes.SYSTEM_ERROR,
                exception.Message,
                new Dictionary<string, object>
                {
                    ["ExceptionType"] = exception.GetType().Name,
                    ["StackTrace"] = exception.StackTrace ?? string.Empty
                });
            return new(false, error);
        }

        /// <summary>
        /// Executes an action if the result is successful
        /// </summary>
        public Result OnSuccess(Action action)
        {
            if (IsSuccess)
                action();
            return this;
        }

        /// <summary>
        /// Executes an action if the result is a failure
        /// </summary>
        public Result OnFailure(Action<Error?> action)
        {
            if (IsFailure)
                action(Error);
            return this;
        }

        /// <summary>
        /// Transforms this result to another result type
        /// </summary>
        public Result<T> Map<T>(Func<T> mapper)
        {
            if (IsFailure)
                return Result<T>.Failure(Error!);
                
            try
            {
                var value = mapper();
                return Result<T>.Success(value);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, ex.Message));
            }
        }

        /// <summary>
        /// Implicit conversion from Result to bool for easy conditional checks
        /// </summary>
        public static implicit operator bool(Result result) => result.IsSuccess;
    }

    /// <summary>
    /// Generic result for operations that return a value
    /// Represents success with a value or failure with error information
    /// </summary>
    /// <typeparam name="T">Type of the value returned on success</typeparam>
    public class Result<T> : Result, IResult<T>
    {
        public T? Value { get; }

        private Result(bool isSuccess, T? value = default, Error? error = null, IReadOnlyCollection<Error>? errors = null)
            : base(isSuccess, error, errors)
        {
            Value = value;
        }

        /// <summary>
        /// Creates a successful result with a value
        /// </summary>
        public static Result<T> Success(T value) => new(true, value);

        /// <summary>
        /// Creates a failed result with a single error
        /// </summary>
        public static new Result<T> Failure(Error error) => new(false, default, error);

        /// <summary>
        /// Creates a failed result with multiple errors
        /// </summary>
        public static new Result<T> Failure(IEnumerable<Error> errors)
        {
            var errorList = errors.ToList();
            return new(false, default, errorList.FirstOrDefault(), errorList);
        }

        /// <summary>
        /// Creates a failed result from an exception
        /// </summary>
        public static new Result<T> Failure(Exception exception)
        {
            var error = Error.System(
                Error.Codes.SYSTEM_ERROR,
                exception.Message,
                new Dictionary<string, object>
                {
                    ["ExceptionType"] = exception.GetType().Name,
                    ["StackTrace"] = exception.StackTrace ?? string.Empty
                });
            return new(false, default, error);
        }

        /// <summary>
        /// Executes an action with the value if the result is successful
        /// </summary>
        public Result<T> OnSuccess(Action<T> action)
        {
            if (IsSuccess && Value is not null)
                action(Value);
            return this;
        }

        /// <summary>
        /// Transforms the value if the result is successful
        /// </summary>
        public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
        {
            if (IsFailure)
                return Result<TOut>.Failure(Error!);

            if (Value is null)
                return Result<TOut>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Value cannot be null"));

            try
            {
                var mappedValue = mapper(Value);
                return Result<TOut>.Success(mappedValue);
            }
            catch (Exception ex)
            {
                return Result<TOut>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, ex.Message));
            }
        }

        /// <summary>
        /// Binds this result with another result-returning function
        /// Enables chaining of result-based operations
        /// </summary>
        public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
        {
            if (IsFailure)
                return Result<TOut>.Failure(Error!);

            if (Value is null)
                return Result<TOut>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, "Value cannot be null"));

            try
            {
                return binder(Value);
            }
            catch (Exception ex)
            {
                return Result<TOut>.Failure(Error.System(Error.Codes.SYSTEM_ERROR, ex.Message));
            }
        }

        /// <summary>
        /// Matches the result with success or failure handlers
        /// Functional pattern matching approach
        /// </summary>
        public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
        {
            if (IsSuccess && Value is not null)
                return onSuccess(Value);
                
            return onFailure(Error ?? Error.System(Error.Codes.SYSTEM_ERROR, "Unknown error"));
        }

        /// <summary>
        /// Implicit conversion from value to successful result
        /// </summary>
        public static implicit operator Result<T>(T value) => Success(value);

        /// <summary>
        /// Implicit conversion from error to failed result
        /// </summary>
        public static implicit operator Result<T>(Error error) => Failure(error);

        /// <summary>
        /// Gets the value or throws if the result is a failure
        /// Use with caution - prefer pattern matching or explicit checks
        /// </summary>
        public T GetValueOrThrow()
        {
            if (IsFailure)
                throw new InvalidOperationException($"Cannot get value from failed result: {Error}");
                
            return Value ?? throw new InvalidOperationException("Value cannot be null in successful result");
        }

        /// <summary>
        /// Gets the value or returns a default value if the result is a failure
        /// </summary>
        public T GetValueOrDefault(T defaultValue = default!)
        {
            return IsSuccess && Value is not null ? Value : defaultValue;
        }
    }

    /// <summary>
    /// Static class providing utility methods for working with results
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Combines multiple results into a single result
        /// Succeeds only if all results are successful
        /// </summary>
        public static Result Combine(params Result[] results)
        {
            var failures = results.Where(r => r.IsFailure).ToList();
            
            if (!failures.Any())
                return Result.Success();

            var errors = failures.SelectMany(f => f.Errors ?? new[] { f.Error! }).ToList();
            return Result.Failure(errors);
        }

        /// <summary>
        /// Ensures a condition is met, returning success or failure result
        /// </summary>
        public static Result Ensure(bool condition, Error error)
        {
            return condition ? Result.Success() : Result.Failure(error);
        }

        /// <summary>
        /// Tries to execute an operation and returns a result
        /// </summary>
        public static Result<T> Try<T>(Func<T> operation)
        {
            try
            {
                var value = operation();
                return Result<T>.Success(value);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(ex);
            }
        }

        /// <summary>
        /// Tries to execute an async operation and returns a result
        /// </summary>
        public static async Task<Result<T>> TryAsync<T>(Func<Task<T>> operation)
        {
            try
            {
                var value = await operation();
                return Result<T>.Success(value);
            }
            catch (Exception ex)
            {
                return Result<T>.Failure(ex);
            }
        }
    }
}