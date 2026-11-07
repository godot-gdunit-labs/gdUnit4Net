// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Asserts;

using System.Numerics;

using Constraints;

internal sealed class NumberAssert<TValue> : AssertBase<TValue, INumberConstraint<TValue>>, INumberAssert<TValue>
    where TValue : IComparable, IComparable<TValue>, IEquatable<TValue>,
    IAdditionOperators<TValue, TValue, TValue>, ISubtractionOperators<TValue, TValue, TValue>
{
    internal NumberAssert(TValue current)
        : base(current)
    {
    }

    public INumberConstraint<TValue> IsBetween(TValue min, TValue max)
    {
        if (Current?.CompareTo(min) < 0 || Current?.CompareTo(max) > 0)
            ThrowTestFailureReport(AssertFailures.IsBetween(Current, min, max), Current, new[] { min, max });
        return this;
    }

    public INumberConstraint<TValue> IsEven()
    {
        if (Convert.ToInt64(Current) % 2 != 0)
            ThrowTestFailureReport(AssertFailures.IsEven(Current), Current, null);
        return this;
    }

    public INumberConstraint<TValue> IsGreater(TValue expected)
    {
        if (Current?.CompareTo(expected) <= 0)
            ThrowTestFailureReport(AssertFailures.IsGreater(Current, expected), Current, expected);
        return this;
    }

    public INumberConstraint<TValue> IsGreaterEqual(TValue expected)
    {
        if (Current?.CompareTo(expected) < 0)
            ThrowTestFailureReport(AssertFailures.IsGreaterEqual(Current, expected), Current, expected);
        return this;
    }

    public INumberConstraint<TValue> IsEqualApprox(TValue expected, TValue approx)
        => IsBetween(expected - approx, expected + approx);

    public INumberConstraint<TValue> IsIn(params TValue[] expected)
    {
        if (Array.IndexOf(expected, Current) == -1)
            ThrowTestFailureReport(AssertFailures.IsIn(Current, expected), Current, expected);
        return this;
    }

    public INumberConstraint<TValue> IsLess(TValue expected)
    {
        if (Current?.CompareTo(expected) >= 0)
            ThrowTestFailureReport(AssertFailures.IsLess(Current, expected), Current, expected);
        return this;
    }

    public INumberConstraint<TValue> IsLessEqual(TValue expected)
    {
        if (Current?.CompareTo(expected) > 0)
            ThrowTestFailureReport(AssertFailures.IsLessEqual(Current, expected), Current, expected);
        return this;
    }

    public INumberConstraint<TValue> IsNegative()
    {
        if (Current?.CompareTo(0) >= 0)
            ThrowTestFailureReport(AssertFailures.IsNegative(Current), Current, null);
        return this;
    }

    public INumberConstraint<TValue> IsNotIn(params TValue[] expected)
    {
        if (Array.IndexOf(expected, Current) != -1)
            ThrowTestFailureReport(AssertFailures.IsNotIn(Current, expected), Current, expected);
        return this;
    }

    public INumberConstraint<TValue> IsNotNegative()
    {
        if (Current?.CompareTo(0) < 0)
            ThrowTestFailureReport(AssertFailures.IsNotNegative(Current), Current, null);
        return this;
    }

    public INumberConstraint<TValue> IsNotZero()
    {
        if (Convert.ToInt64(Current) == 0)
            ThrowTestFailureReport(AssertFailures.IsNotZero(), Current, null);
        return this;
    }

    public INumberConstraint<TValue> IsOdd()
    {
        if (Convert.ToInt64(Current) % 2 == 0)
            ThrowTestFailureReport(AssertFailures.IsOdd(Current), Current, null);
        return this;
    }

    public INumberConstraint<TValue> IsZero()
    {
        if (Convert.ToInt64(Current) != 0)
            ThrowTestFailureReport(AssertFailures.IsZero(Current), Current, null);
        return this;
    }
}
