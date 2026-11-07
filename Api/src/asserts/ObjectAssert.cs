// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Asserts;

using Constraints;

/// <inheritdoc cref="IObjectAssert{TValue}" />
public sealed class ObjectAssert<TValue> : AssertBase<TValue, IObjectConstraint<TValue>>, IObjectAssert<TValue>
{
    internal ObjectAssert(TValue? current)
        : base(current)
    {
        var type = current?.GetType();
        if (type is { IsPrimitive: true })
            ThrowTestFailureReport($"ObjectAssert initial error: current is primitive <{type}>", Current, null);
    }

    /// <inheritdoc />
    public IObjectConstraint<TValue> IsNotInstanceOf<TExpectedType>()
    {
        if (Current is TExpectedType)
            ThrowTestFailureReport(AssertFailures.NotInstanceOf(typeof(TExpectedType)), Current, typeof(TExpectedType));
        return this;
    }

    /// <inheritdoc />
    public IObjectConstraint<TValue> IsNotSame(object expected)
    {
        if (ReferenceEquals(expected, Current))
            ThrowTestFailureReport(AssertFailures.IsNotSame(expected), Current, expected);
        return this;
    }

    /// <inheritdoc />
    public IObjectConstraint<TValue> IsSame(object expected)
    {
        if (!ReferenceEquals(expected, Current))
            ThrowTestFailureReport(AssertFailures.IsSame(Current, expected), Current, expected);
        return this;
    }

    /// <inheritdoc />
    public IObjectConstraint<TValue> IsInstanceOf<TExpectedType>()
    {
        if (Current is not TExpectedType)
            ThrowTestFailureReport(AssertFailures.IsInstanceOf(Current?.GetType(), typeof(TExpectedType)), Current, typeof(TExpectedType));
        return this;
    }
}
