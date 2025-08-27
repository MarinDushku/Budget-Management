// Result Pattern Interface - Foundation for Modern Error Handling
// File: Shared/Core/IResult.cs

using System;
using System.Collections.Generic;

namespace BudgetManagement.Shared.Core
{
    /// <summary>
    /// Base interface for result types representing operation outcomes
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// Indicates whether the operation failed
        /// </summary>
        bool IsFailure { get; }

        /// <summary>
        /// Error information when the operation fails
        /// </summary>
        Error? Error { get; }

        /// <summary>
        /// Collection of validation errors if applicable
        /// </summary>
        IReadOnlyCollection<Error>? Errors { get; }
    }

    /// <summary>
    /// Generic result interface for operations that return a value
    /// </summary>
    /// <typeparam name="T">Type of the value returned on success</typeparam>
    public interface IResult<out T> : IResult
    {
        /// <summary>
        /// The value returned on successful operation
        /// </summary>
        T? Value { get; }
    }
}