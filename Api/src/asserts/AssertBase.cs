// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Asserts;

using Core.Execution.Exceptions;
using Core.Extensions;

/// <inheritdoc cref="IAssertBase{TValue,TAssert}" />
public abstract class AssertBase<TValue, TAssert> : IAssertBase<TValue, TAssert>, IAssertMessage<TAssert>
    where TAssert : IAssert
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="AssertBase{TValue, TAssert}" /> class.
    /// </summary>
    /// <param name="current">The current value to be inspected.</param>
    protected AssertBase(TValue? current) => Current = current;

#pragma warning disable CS1591, SA1600 // Missing XML comment for publicly visible type or member
    protected TValue? Current { get; }

    protected string? CustomFailureMessage { get; private set; }

    protected string CurrentFailureMessage { get; set; } = string.Empty;

    protected string AppendingFailureMessage { get; set; } = string.Empty;

    private TAssert Self => (TAssert)(object)this;
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <inheritdoc />
    public TAssert IsEqual(TValue expected)
    {
        var result = Comparable.IsEqual(Current, expected);
        if (!result.Valid)
            ThrowTestFailureReport(AssertFailures.IsEqual(Current, expected), Current, expected);
        return Self;
    }

    /// <inheritdoc />
    public TAssert IsNotEqual(TValue expected)
    {
        var result = Comparable.IsEqual(Current, expected);
        if (result.Valid)
            ThrowTestFailureReport(AssertFailures.IsNotEqual(Current, expected), Current, expected);
        return Self;
    }

    /// <inheritdoc />
    public TAssert IsNull()
    {
        if (Current != null)
            ThrowTestFailureReport(AssertFailures.IsNull(Current), Current, null);
        return Self;
    }

    /// <inheritdoc />
    public TAssert IsNotNull()
    {
        if (Current == null)
            ThrowTestFailureReport(AssertFailures.IsNotNull(), Current, null);
        return Self;
    }

    /// <inheritdoc />
    public TAssert OverrideFailureMessage(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        CustomFailureMessage = message;
        return Self;
    }

    /// <inheritdoc />
    public TAssert AppendFailureMessage(string message)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
        AppendingFailureMessage = message;
        return Self;
    }

    internal static bool IsSame<TLeft, TRight>(TLeft lKey, TRight rKey)
    {
        var left = lKey.UnboxVariant();
        var right = rKey.UnboxVariant();

        if ((left is string || left?.GetType().IsPrimitive) ?? false)
            return Equals(left, right);
        return ReferenceEquals(left, right);
    }

#pragma warning disable IDE0060 // Remove unused parameter
    internal void ThrowTestFailureReport(string message, object? current, object? expected)
#pragma warning restore IDE0060 // Remove unused parameter
    {
#pragma warning disable CA1062
        var failureMessage = (CustomFailureMessage ?? message).UnixFormat();
#pragma warning restore CA1062
        if (AppendingFailureMessage.Length != 0)
        {
            failureMessage = $"""
                              {failureMessage}

                              Additional info:
                              {AppendingFailureMessage}
                              """;
        }

        CurrentFailureMessage = failureMessage;
        throw new TestFailedException(failureMessage);
    }
}
#pragma warning restore CS1591, SA1600
