// MediatR Validation Behavior - Cross-Cutting Concern Implementation
// File: Shared/Infrastructure/ValidationBehavior.cs

using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using BudgetManagement.Shared.Core;

namespace BudgetManagement.Shared.Infrastructure
{
    /// <summary>
    /// MediatR behavior for automatic request validation using FluentValidation
    /// Implements cross-cutting validation concerns for all commands and queries
    /// </summary>
    /// <typeparam name="TRequest">Request type</typeparam>
    /// <typeparam name="TResponse">Response type</typeparam>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

        public ValidationBehavior(
            IEnumerable<IValidator<TRequest>> validators,
            ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        {
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TResponse> Handle(
            TRequest request, 
            RequestHandlerDelegate<TResponse> next, 
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestId = GetRequestId(request);

            // Skip validation if no validators are registered
            if (!_validators.Any())
            {
                _logger.LogDebug("No validators found for {RequestName} [{RequestId}], skipping validation", 
                    requestName, requestId);
                return await next();
            }

            _logger.LogDebug("Validating request {RequestName} [{RequestId}] using {ValidatorCount} validator(s)",
                requestName, requestId, _validators.Count());

            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["RequestName"] = requestName,
                ["RequestId"] = requestId,
                ["ValidatorCount"] = _validators.Count()
            });

            try
            {
                // Create validation context
                var context = new ValidationContext<TRequest>(request);

                // Run all validators in parallel for better performance
                var validationTasks = _validators
                    .Select(validator => validator.ValidateAsync(context, cancellationToken))
                    .ToArray();

                var validationResults = await Task.WhenAll(validationTasks);

                // Collect all failures
                var failures = validationResults
                    .SelectMany(result => result.Errors)
                    .Where(error => error != null)
                    .ToList();

                if (failures.Any())
                {
                    _logger.LogWarning("Validation failed for {RequestName} [{RequestId}] with {FailureCount} error(s)",
                        requestName, requestId, failures.Count);

                    // Log individual validation errors
                    foreach (var failure in failures)
                    {
                        _logger.LogWarning("Validation error in {RequestName} [{RequestId}]: {PropertyName} = {AttemptedValue}, Error: {ErrorMessage}",
                            requestName, requestId, failure.PropertyName, failure.AttemptedValue, failure.ErrorMessage);
                    }

                    // Handle different response types
                    if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                    {
                        // For Result<T> responses, return a validation failure
                        var validationError = CreateValidationError(failures);
                        return CreateFailureResult<TResponse>(validationError);
                    }
                    else if (typeof(TResponse) == typeof(Result))
                    {
                        // For Result responses, return a validation failure
                        var validationError = CreateValidationError(failures);
                        return (TResponse)(object)Result.Failure(validationError);
                    }
                    else
                    {
                        // For other response types, throw a validation exception
                        throw new ValidationException("Validation failed", failures);
                    }
                }

                _logger.LogDebug("Validation passed for {RequestName} [{RequestId}]", requestName, requestId);
                return await next();
            }
            catch (ValidationException)
            {
                // Re-throw validation exceptions as they are expected
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during validation of {RequestName} [{RequestId}]", 
                    requestName, requestId);
                throw;
            }
        }

        /// <summary>
        /// Creates a validation error from FluentValidation failures
        /// </summary>
        private static Error CreateValidationError(IList<FluentValidation.Results.ValidationFailure> failures)
        {
            var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
            var metadata = new Dictionary<string, object>();

            // Group failures by property for better error reporting
            var failuresByProperty = failures.GroupBy(f => f.PropertyName);
            foreach (var group in failuresByProperty)
            {
                metadata[group.Key] = group.Select(f => f.ErrorMessage).ToArray();
            }

            return Error.Validation(
                Error.Codes.VALIDATION_ERROR,
                $"Validation failed: {errorMessage}",
                metadata);
        }

        /// <summary>
        /// Creates a failure result for generic Result<T> types using reflection
        /// </summary>
        private static TResponse CreateFailureResult<TResponse>(Error error)
        {
            var responseType = typeof(TResponse);
            
            if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var valueType = responseType.GetGenericArguments()[0];
                var resultType = typeof(Result<>).MakeGenericType(valueType);
                var failureMethod = resultType.GetMethod("Failure", new[] { typeof(Error) });
                
                if (failureMethod != null)
                {
                    var result = failureMethod.Invoke(null, new object[] { error });
                    return (TResponse)result!;
                }
            }

            throw new InvalidOperationException($"Cannot create failure result for type {responseType.Name}");
        }

        /// <summary>
        /// Extracts a unique identifier from the request for correlation
        /// </summary>
        private static string GetRequestId(TRequest request)
        {
            // Try to get ID from command/query interfaces
            if (request is ICommand command)
                return command.CommandId.ToString();

            if (request is IQuery<object> query)
                return query.QueryId.ToString();

            // Fallback to hash code
            return request.GetHashCode().ToString();
        }
    }

    /// <summary>
    /// Extension methods for validation behavior
    /// </summary>
    public static class ValidationBehaviorExtensions
    {
        /// <summary>
        /// Validates a request manually and returns detailed validation results
        /// </summary>
        public static async Task<FluentValidation.Results.ValidationResult> ValidateAsync<T>(
            this IValidator<T> validator,
            T instance,
            string? ruleSet = null,
            CancellationToken cancellationToken = default)
        {
            var context = new ValidationContext<T>(instance);
            
            if (!string.IsNullOrEmpty(ruleSet))
            {
                context.RootContextData["ruleset"] = ruleSet;
            }

            return await validator.ValidateAsync(context, cancellationToken);
        }

        /// <summary>
        /// Validates a request and throws if validation fails
        /// </summary>
        public static async Task ValidateAndThrowAsync<T>(
            this IValidator<T> validator,
            T instance,
            string? ruleSet = null,
            CancellationToken cancellationToken = default)
        {
            var result = await validator.ValidateAsync(instance, ruleSet, cancellationToken);
            
            if (!result.IsValid)
            {
                throw new ValidationException("Validation failed", result.Errors);
            }
        }

        /// <summary>
        /// Converts FluentValidation result to Result pattern
        /// </summary>
        public static Result ToResult(this FluentValidation.Results.ValidationResult validationResult)
        {
            if (validationResult.IsValid)
                return Result.Success();

            var errorMessage = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            var metadata = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key, 
                    g => (object)g.Select(e => e.ErrorMessage).ToArray());

            var error = Error.Validation(
                Error.Codes.VALIDATION_ERROR,
                $"Validation failed: {errorMessage}",
                metadata);

            return Result.Failure(error);
        }
    }
}