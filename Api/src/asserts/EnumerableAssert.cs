// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Asserts;

using System.Collections;

using Constraints;

using Core.Extensions;

using Extractors;

using Array = Godot.Collections.Array;

/// <inheritdoc cref="IEnumerableAssert{TValue}" />
public sealed class EnumerableAssert<TValue> : AssertBase<IEnumerable<TValue?>, IEnumerableConstraint<TValue?>>, IEnumerableAssert<TValue?>
{
    internal EnumerableAssert(IEnumerable? current)
        : base(current?.Cast<TValue?>())
    {
    }

    internal EnumerableAssert(IEnumerable<TValue?>? current)
        : base(current)
    {
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> IsEqualIgnoringCase(IEnumerable<TValue?> expected)
    {
        var result = Comparable.IsEqual(Current, expected, GodotObjectExtensions.Mode.CaseInsensitive);
        if (!result.Valid)
            ThrowTestFailureReport(AssertFailures.IsEqualIgnoringCase(Current, expected), Current, expected);
        return this;
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> IsNotEqualIgnoringCase(IEnumerable<TValue?> expected)
    {
        var result = Comparable.IsEqual(Current, expected, GodotObjectExtensions.Mode.CaseInsensitive);
        if (result.Valid)
            ThrowTestFailureReport(AssertFailures.IsNotEqualIgnoringCase(Current, expected), Current, expected);
        return this;
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> IsEmpty()
    {
        var count = Current?.Count() ?? -1;
        if (count != 0)
            ThrowTestFailureReport(AssertFailures.IsEmpty(count, Current == null), Current, count);
        return this;
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> IsNotEmpty()
    {
        var count = Current?.Count() ?? -1;
        if (count == 0)
            ThrowTestFailureReport(AssertFailures.IsNotEmpty(), Current, null);
        return this;
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> IsSame(IEnumerable<TValue?> expected)
    {
        if (!ReferenceEquals(Current, expected))
            ThrowTestFailureReport(AssertFailures.IsSame(Current, expected), Current, expected);
        return this;
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> IsNotSame(IEnumerable<TValue?> expected)
    {
        if (ReferenceEquals(Current, expected))
            ThrowTestFailureReport(AssertFailures.IsNotSame(expected), Current, expected);
        return this;
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> HasSize(int expected)
    {
        var count = Current?.Count();
        if (count != expected)
            ThrowTestFailureReport(AssertFailures.HasSize(count, expected), Current, null);
        return this;
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> Contains(params TValue?[] expected)
        => CheckContains([.. expected], false);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> Contains(IEnumerable<TValue?> expected)
        => CheckContains(expected, false);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> Contains(Array expected)
    {
        var expected_ = expected as IEnumerable<TValue?>;
        return CheckContains(expected_!, false);
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsSame(params TValue?[] expected)
        => CheckContains([.. expected], true);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsSame(IEnumerable<TValue?> expected)
        => CheckContains(expected, true);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsSame(Array expected)
    {
        var expected_ = expected as IEnumerable<TValue?>;
        return CheckContains(expected_!, true);
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsExactly(params TValue?[] expected)
        => CheckContainsExactly([.. expected], false);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsExactly(IEnumerable<TValue?> expected)
        => CheckContainsExactly(expected, false);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsExactly(Array expected)
    {
        var expected_ = expected as IEnumerable<TValue?>;
        return CheckContainsExactly(expected_!, false);
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsSameExactly(params TValue?[] expected)
        => CheckContainsExactly([.. expected], true);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsSameExactly(IEnumerable<TValue?> expected)
        => CheckContainsExactly(expected, true);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsSameExactly(Array expected)
    {
        var expected_ = expected as IEnumerable<TValue?>;
        return CheckContainsExactly(expected_!, true);
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsExactlyInAnyOrder(params TValue?[] expected)
        => CheckContainsExactlyInAnyOrder([.. expected], false);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsExactlyInAnyOrder(IEnumerable<TValue?> expected)
        => CheckContainsExactlyInAnyOrder(expected, false);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsExactlyInAnyOrder(Array expected)
    {
        var expected_ = expected as IEnumerable<TValue?>;
        return CheckContainsExactlyInAnyOrder(expected_!, false);
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsSameExactlyInAnyOrder(params TValue?[] expected)
        => CheckContainsExactlyInAnyOrder([.. expected], true);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsSameExactlyInAnyOrder(IEnumerable<TValue?> expected)
        => CheckContainsExactlyInAnyOrder(expected, true);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> ContainsSameExactlyInAnyOrder(Array expected)
    {
        var expected_ = expected as IEnumerable<TValue?>;
        return CheckContainsExactlyInAnyOrder(expected_!, true);
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> NotContains(params TValue?[] expected)
        => CheckNotContains([.. expected], false);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> NotContains(IEnumerable<TValue?> expected)
        => CheckNotContains(expected, false);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> NotContains(Array expected)
    {
        var expected_ = expected as IEnumerable<TValue?>;
        return CheckNotContains(expected_!, false);
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> NotContainsSame(params TValue?[] expected)
        => CheckNotContains([.. expected], true);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> NotContainsSame(IEnumerable<TValue?> expected)
        => CheckNotContains(expected, true);

    /// <inheritdoc/>
    public IEnumerableConstraint<TValue?> NotContainsSame(Array expected)
    {
        var expected_ = expected as IEnumerable<TValue?>;
        return CheckNotContains(expected_!, true);
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<object?> Extract(string methodName, params object[] args)
    {
        ArgumentException.ThrowIfNullOrEmpty(methodName);
        return ExtractV(new ValueExtractor(methodName, args));
    }

    /// <inheritdoc/>
    public IEnumerableConstraint<object?> ExtractV(params IValueExtractor[] extractors)
        => new EnumerableAssert<object?>(
            Current?.Select(v =>
                {
                    var values = extractors.Select(e => e.ExtractValue(v)).ToArray();
                    return values.Length == 1 ? values.First() : new Tuple(values);
                })
                .ToList());

    private EnumerableAssert<TValue?> CheckContains(IEnumerable<TValue?> expected, bool referenceEquals)
    {
        var expectedList = expected.ToList();

        // we test for contains nothing
        if (expectedList.Count == 0)
            return this;

        // Create list once to avoid multiple enumerations
        var notFound = expectedList.Where(expectedItem =>
                Current?.Any(currentItem =>
                    referenceEquals
                        ? expectedItem.IsSame<TValue, EnumerableAssert<TValue?>>(currentItem)
                        : expectedItem.IsEquals(currentItem)) != true) // This pattern maintains the original null handling
            .ToList();

        if (notFound.Count > 0)
            ThrowTestFailureReport(AssertFailures.Contains(Current, expectedList, notFound), Current, expected);
        return this;
    }

    private EnumerableAssert<TValue?> CheckContainsExactly(IEnumerable<TValue?> expected, bool referenceEquals)
    {
        var expectedList = expected.ToList();

        // we test for contains nothing
        if (expectedList.Count == 0)
            return this;

        var diff = DiffArrayExactly(Current, expectedList, referenceEquals);
        var notExpected = diff.NotExpected;
        var notFound = diff.NotFound;
        if (notExpected.Count != 0 || notFound.Count != 0)
            ThrowTestFailureReport(AssertFailures.ContainsExactly(Current, expectedList, notFound, notExpected), Current, expected);
        return this;
    }

    private EnumerableAssert<TValue?> CheckContainsExactlyInAnyOrder(IEnumerable<TValue?> expected, bool referenceEquals)
    {
        var expectedList = expected.ToList();

        // we test for contains nothing
        if (expectedList.Count == 0)
            return this;

        var diff = DiffArrayAnyOrder(Current, expectedList, referenceEquals);
        var notExpected = diff.NotExpected;
        var notFound = diff.NotFound;

        // no difference and additions found
        if (notExpected.Count != 0 || notFound.Count != 0)
            ThrowTestFailureReport(AssertFailures.ContainsExactlyInAnyOrder(Current, expectedList, notFound, notExpected), Current, expected);
        return this;
    }

    private EnumerableAssert<TValue?> CheckNotContains(IEnumerable<TValue?> expected, bool referenceEquals)
    {
        var expectedList = expected.ToList();

        // we test for contains nothing
        if (expectedList.Count == 0)
            return this;

        var found = Current?
            .Where(currentItem =>
                expectedList.Any(expectedItem =>
                    referenceEquals ? currentItem.IsSame<TValue, EnumerableAssert<TValue?>>(expectedItem) : currentItem.IsEquals(expectedItem)))
            .ToList() ?? [];

        if (found.Count != 0)
            ThrowTestFailureReport(AssertFailures.NotContains(Current, expectedList, found), Current, expected);
        return this;
    }

    private ArrayDiff DiffArrayAnyOrder(
        IEnumerable<TValue?>? current,
        IEnumerable<TValue?>? expected,
        bool referenceEquals = false)
    {
        var ll = current?.ToArray() ?? [];
        var rr = expected?.ToArray() ?? [];

        var notExpected = ll.Where(left =>
            !rr.Any(e => referenceEquals ? left.IsSame<TValue, EnumerableAssert<TValue?>>(e) : left.IsEquals(e))).ToList();
        var notFound = rr.Where(right =>
            !ll.Any(e => referenceEquals ? right.IsSame<TValue, EnumerableAssert<TValue?>>(e) : right.IsEquals(e))).ToList();

        return new ArrayDiff
        {
            NotExpected = notExpected,
            NotFound = notFound
        };
    }

    private ArrayDiff DiffArrayExactly(
        IEnumerable<TValue?>? current,
        IEnumerable<TValue?>? expected,
        bool referenceEquals = false)
    {
        var ll = current?.ToArray() ?? [];
        var rr = expected?.ToArray() ?? [];

        var notExpected = new List<TValue?>();
        var notFound = new List<TValue?>();

        for (var i = 0; i < ll.Length; i++)
        {
            var left = ll[i];
            if (i >= rr.Length)
            {
                notExpected.Add(left);
                continue;
            }

            var right = rr[i];
            if (!(referenceEquals ? left.IsSame<TValue, EnumerableAssert<TValue?>>(right) : left.IsEquals(right)))
                notExpected.Add(left);
        }

        for (var i = 0; i < rr.Length; i++)
        {
            var right = rr[i];
            if (i >= ll.Length)
            {
                notFound.Add(right);
                continue;
            }

            var left = ll[i];
            if (!(referenceEquals ? right.IsSame<TValue, EnumerableAssert<TValue?>>(left) : right.IsEquals(left)))
                notFound.Add(right);
        }

        return new ArrayDiff
        {
            NotExpected = notExpected,
            NotFound = notFound
        };
    }

    private class ArrayDiff
    {
        public List<TValue?> NotExpected { get; init; } = [];

        public List<TValue?> NotFound { get; init; } = [];
    }
}
