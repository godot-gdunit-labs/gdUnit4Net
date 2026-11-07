// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Data;

using System;

/// <summary>
///     Exception thrown when an asynchronous data point operation is canceled due to timeout or cancellation request.
/// </summary>
/// <remarks>
///     <para>
///         This exception is specifically designed for data-driven test scenarios where async data sources
///         may timeout or be canceled during execution. It provides enhanced stack trace information
///         to help identify the source of the cancellation.
///     </para>
/// </remarks>
/// <seealso cref="OperationCanceledException" />
/// <seealso cref="DataPointValueProvider" />
public sealed class AsyncDataPointCanceledException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncDataPointCanceledException" /> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the cancellation reason.</param>
    public AsyncDataPointCanceledException(string message)
        : base(message) => StackTrace = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncDataPointCanceledException" /> class with a specified error message and custom stack trace.
    /// </summary>
    /// <param name="message">The message that describes the cancellation reason.</param>
    /// <param name="stackTrace">The custom stack trace information showing the original source location.</param>
    public AsyncDataPointCanceledException(string message, string stackTrace)
        : base(message) => StackTrace = stackTrace;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncDataPointCanceledException" /> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the cancellation reason.</param>
    /// <param name="innerException">The exception that caused the current cancellation typically an <see cref="OperationCanceledException" />.</param>
    public AsyncDataPointCanceledException(string message, Exception innerException)
        : base(message, innerException) => StackTrace = string.Empty;

    /// <summary>
    ///     Initializes a new instance of the <see cref="AsyncDataPointCanceledException" /> class.
    ///     This constructor is used for serialization.
    /// </summary>
    private AsyncDataPointCanceledException() => StackTrace = string.Empty;

    /// <summary>
    ///     Gets the custom stack trace information for this exception.
    /// </summary>
    /// <value>
    ///     A string containing stack trace information, or an empty string if no custom stack trace was provided.
    /// </value>
    /// <remarks>
    ///     This property overrides the base <see cref="Exception.StackTrace" /> to provide more meaningful
    ///     location information when the exception originates from async data point operations.
    /// </remarks>
    public override string StackTrace { get; }
}
