// Error Types for Result Pattern - Modern Error Handling
// File: Shared/Core/Error.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace BudgetManagement.Shared.Core
{
    /// <summary>
    /// Represents an error that occurred during operation execution
    /// Immutable record type for functional error handling
    /// </summary>
    public record Error
    {
        /// <summary>
        /// Unique code identifying the error type
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Human-readable error message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Error severity level
        /// </summary>
        public ErrorType Type { get; }

        /// <summary>
        /// Additional error metadata
        /// </summary>
        public Dictionary<string, object>? Metadata { get; }

        private Error(string code, string message, ErrorType type, Dictionary<string, object>? metadata = null)
        {
            Code = code;
            Message = message;
            Type = type;
            Metadata = metadata;
        }

        /// <summary>
        /// Creates a validation error
        /// </summary>
        public static Error Validation(string code, string message, Dictionary<string, object>? metadata = null)
            => new(code, message, ErrorType.Validation, metadata);

        /// <summary>
        /// Creates a business rule violation error
        /// </summary>
        public static Error Business(string code, string message, Dictionary<string, object>? metadata = null)
            => new(code, message, ErrorType.Business, metadata);

        /// <summary>
        /// Creates a not found error
        /// </summary>
        public static Error NotFound(string code, string message, Dictionary<string, object>? metadata = null)
            => new(code, message, ErrorType.NotFound, metadata);

        /// <summary>
        /// Creates an unauthorized access error
        /// </summary>
        public static Error Unauthorized(string code, string message, Dictionary<string, object>? metadata = null)
            => new(code, message, ErrorType.Unauthorized, metadata);

        /// <summary>
        /// Creates a system/infrastructure error
        /// </summary>
        public static Error System(string code, string message, Dictionary<string, object>? metadata = null)
            => new(code, message, ErrorType.System, metadata);

        /// <summary>
        /// Creates a conflict error (e.g., duplicate entries)
        /// </summary>
        public static Error Conflict(string code, string message, Dictionary<string, object>? metadata = null)
            => new(code, message, ErrorType.Conflict, metadata);

        /// <summary>
        /// Creates a validation error from multiple validation failures
        /// </summary>
        public static Error Validation(IEnumerable<string> errors)
        {
            var errorList = errors.ToList();
            var message = string.Join("; ", errorList);
            var metadata = new Dictionary<string, object>
            {
                ["ValidationErrors"] = errorList
            };
            
            return new("VALIDATION_FAILED", message, ErrorType.Validation, metadata);
        }

        /// <summary>
        /// Common error codes used throughout the application
        /// </summary>
        public static class Codes
        {
            // General
            public const string VALIDATION_FAILED = "VALIDATION_FAILED";
            public const string VALIDATION_ERROR = "VALIDATION_ERROR";
            public const string NOT_FOUND = "NOT_FOUND";
            public const string UNAUTHORIZED = "UNAUTHORIZED";
            public const string SYSTEM_ERROR = "SYSTEM_ERROR";
            public const string CONFLICT = "CONFLICT";

            // Budget specific
            public const string INVALID_AMOUNT = "INVALID_AMOUNT";
            public const string INVALID_DATE = "INVALID_DATE";
            public const string CATEGORY_NOT_FOUND = "CATEGORY_NOT_FOUND";
            public const string DUPLICATE_ENTRY = "DUPLICATE_ENTRY";
            public const string INSUFFICIENT_BUDGET = "INSUFFICIENT_BUDGET";
            public const string DATABASE_CONNECTION_FAILED = "DATABASE_CONNECTION_FAILED";
            public const string EXPORT_FAILED = "EXPORT_FAILED";

            // Additional system errors used in tests and handlers
            public const string DATABASE_ERROR = "DATABASE_ERROR";
            public const string CACHE_ERROR = "CACHE_ERROR";
        }

        /// <summary>
        /// Implicit conversion to string for easy logging
        /// </summary>
        public static implicit operator string(Error error) => error.ToString();

        /// <summary>
        /// Returns formatted error string
        /// </summary>
        public override string ToString()
        {
            var metadataStr = Metadata != null && Metadata.Any() 
                ? $" | Metadata: {string.Join(", ", Metadata.Select(kv => $"{kv.Key}={kv.Value}"))}"
                : string.Empty;
            
            return $"[{Type}] {Code}: {Message}{metadataStr}";
        }
    }

    /// <summary>
    /// Enumeration of error types for categorization and handling
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// Input validation errors (400 Bad Request equivalent)
        /// </summary>
        Validation = 1,

        /// <summary>
        /// Business rule violations (422 Unprocessable Entity equivalent)
        /// </summary>
        Business = 2,

        /// <summary>
        /// Resource not found errors (404 Not Found equivalent)
        /// </summary>
        NotFound = 3,

        /// <summary>
        /// Access denied errors (401 Unauthorized equivalent)
        /// </summary>
        Unauthorized = 4,

        /// <summary>
        /// System/infrastructure errors (500 Internal Server Error equivalent)
        /// </summary>
        System = 5,

        /// <summary>
        /// Resource conflict errors (409 Conflict equivalent)
        /// </summary>
        Conflict = 6
    }
}