// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Asserts;

using Constraints;

using Core.Extensions;

/// <inheritdoc cref="IStringAssert" />
public sealed class StringAssert : AssertBase<string, IStringConstraint>, IStringAssert
{
    internal StringAssert(string? current)
        : base(current)
    {
    }

    /// <inheritdoc />
    public IStringConstraint Contains(string expected)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        if (Current == null || !Current.Contains(expected, StringComparison.Ordinal))
            ThrowTestFailureReport(AssertFailures.Contains(Current, expected), Current, expected);
        return this;
    }

    /// <inheritdoc />
    public IStringConstraint ContainsIgnoringCase(string expected)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        if (Current == null || !Current.ToLower().Contains(expected.ToLower(), StringComparison.OrdinalIgnoreCase))
            ThrowTestFailureReport(AssertFailures.ContainsIgnoringCase(Current, expected), Current, expected);
        return this;
    }

    /// <inheritdoc />
    public IStringConstraint EndsWith(string expected)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        if (Current == null || !Current.EndsWith(expected))
            ThrowTestFailureReport(AssertFailures.EndsWith(Current, expected), Current, expected);
        return this;
    }

    /// <inheritdoc />
    public IStringConstraint HasLength(int length, IStringAssert.Compare comparator = IStringAssert.Compare.EQUAL)
    {
        if (Current == null)
            ThrowTestFailureReport(AssertFailures.HasLength(-1, length, comparator), Current, length);

        var currentLength = Current?.Length ?? -1;
        var failed = false;
        switch (comparator)
        {
            case IStringAssert.Compare.EQUAL:
                if (currentLength != length)
                    failed = true;
                break;
            case IStringAssert.Compare.GREATER_EQUAL:
                if (currentLength < length)
                    failed = true;
                break;
            case IStringAssert.Compare.GREATER_THAN:
                if (currentLength <= length)
                    failed = true;
                break;
            case IStringAssert.Compare.LESS_EQUAL:
                if (currentLength > length)
                    failed = true;
                break;
            case IStringAssert.Compare.LESS_THAN:
                if (currentLength >= length)
                    failed = true;
                break;

            // ReSharper disable once RedundantEmptySwitchSection
            default:
                break;
        }

        if (failed)
            ThrowTestFailureReport(AssertFailures.HasLength(currentLength, length, comparator), Current, length);
        return this;
    }

    /// <inheritdoc />
    public IStringConstraint IsEmpty()
    {
        if (Current == null || Current.Length > 0)
            ThrowTestFailureReport(AssertFailures.IsEmpty(Current), Current, null);
        return this;
    }

    /// <inheritdoc />
    public IStringConstraint IsEqualIgnoringCase(string expected)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var result = Comparable.IsEqual(Current, expected, GodotObjectExtensions.Mode.CaseInsensitive);
        if (!result.Valid)
            ThrowTestFailureReport(AssertFailures.IsEqualIgnoringCase(Current, expected), Current, expected);
        return this;
    }

    /// <inheritdoc />
    public IStringConstraint IsNotEmpty()
    {
        if (Current?.Length == 0)
            ThrowTestFailureReport(AssertFailures.IsNotEmpty(), Current, null);
        return this;
    }

    /// <inheritdoc />
    public IStringConstraint IsNotEqualIgnoringCase(string expected)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var result = Comparable.IsEqual(Current, expected, GodotObjectExtensions.Mode.CaseInsensitive);
        if (result.Valid)
            ThrowTestFailureReport(AssertFailures.IsNotEqualIgnoringCase(Current, expected), Current, expected);
        return this;
    }

    /// <inheritdoc />
    public IStringConstraint NotContains(string expected)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        if (Current != null && Current.Contains(expected, StringComparison.Ordinal))
            ThrowTestFailureReport(AssertFailures.NotContains(Current, expected), Current, expected);
        return this;
    }

    /// <inheritdoc />
    public IStringConstraint NotContainsIgnoringCase(string expected)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        if (Current != null && Current.ToLower().Contains(expected.ToLower(), StringComparison.OrdinalIgnoreCase))
            ThrowTestFailureReport(AssertFailures.NotContainsIgnoringCase(Current, expected), Current, expected);
        return this;
    }

    /// <inheritdoc />
    public IStringConstraint StartsWith(string expected)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        if (Current == null || !Current.StartsWith(expected))
            ThrowTestFailureReport(AssertFailures.StartsWith(Current, expected), Current, expected);
        return this;
    }
}
