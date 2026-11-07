// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Asserts;

using System.Collections;

using Constraints;

/// <inheritdoc cref="IDictionaryAssert{TKey,TValue}" />
public sealed class DictionaryAssert<TKey, TValue> : AssertBase<IEnumerable, IDictionaryConstraint<TKey, TValue>>, IDictionaryAssert<TKey, TValue>
    where TKey : notnull
{
    internal DictionaryAssert(IDictionary<TKey, TValue>? current)
        : base(current)
    {
    }

    private DictionaryAssert(IDictionary? current)
        : base(current)
    {
    }

    private bool IsGeneric => CurrentTyped != null;

    private new IDictionary? Current => base.Current as IDictionary;

    private IDictionary<TKey, TValue>? CurrentTyped => base.Current as IDictionary<TKey, TValue>;

    private int ItemCount => IsGeneric ? CurrentTyped?.Count ?? 0 : Current?.Count ?? 0;

    private ICollection<TKey> Keys => GetKeys() ?? [];

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> IsEmpty()
    {
        if (Current == null && CurrentTyped == null)
            ThrowTestFailureReport(AssertFailures.IsEmpty(ItemCount, true), ItemCount, null);
        if (ItemCount != 0)
            ThrowTestFailureReport(AssertFailures.IsEmpty(ItemCount, CurrentTyped == null && Current == null), ItemCount, null);
        return this;
    }

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> IsNotEmpty()
    {
        CheckNotNull();
        if (ItemCount == 0)
            ThrowTestFailureReport(AssertFailures.IsNotEmpty(), base.Current, null);
        return this;
    }

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> HasSize(int expected)
    {
        CheckNotNull();
        if (ItemCount != expected)
            ThrowTestFailureReport(AssertFailures.HasSize(base.Current, expected), base.Current, expected);
        return this;
    }

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> ContainsKeys(params TKey[] expected)
    {
        CheckNotNull();
        var notFound = expected.Where(key => !Keys.Contains(key)).ToList();

        if (notFound.Count > 0)
            ThrowTestFailureReport(AssertFailures.Contains(Keys, expected, notFound), base.Current, expected);
        return this;
    }

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> ContainsKeys(IEnumerable expected)
        => ContainsKeys([.. Enumerable.Cast<TKey>(expected)]);

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> NotContainsKeys(params TKey[] expected)
    {
        CheckNotNull();
        var found = expected.Where(Keys.Contains).ToList();
        if (found.Count > 0)
            ThrowTestFailureReport(AssertFailures.NotContains(Keys, expected, found), base.Current, expected);
        return this;
    }

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> NotContainsKeys(IEnumerable expected)
        => NotContainsKeys([.. Enumerable.Cast<TKey>(expected)]);

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> ContainsKeyValue(TKey key, TValue value)
    {
        CheckNotNull();
        var hasKey = Keys.Contains(key);
        var expectedKeyValue = new Dictionary<TKey, TValue> { { key, value } };
        if (!hasKey)
            ThrowTestFailureReport(AssertFailures.ContainsKeyValue(expectedKeyValue), base.Current, expectedKeyValue);

        var currentValue = TryGetValue(key);
        var result = Comparable.IsEqual(currentValue, value);
        if (!result.Valid)
            ThrowTestFailureReport(AssertFailures.ContainsKeyValue(expectedKeyValue, currentValue), base.Current, expectedKeyValue);
        return this;
    }

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> NotContainsSameKeys(params TKey[] expected)
    {
        CheckNotNull();
        var found = expected.Where(key => Keys.Any(k => IsSame(k, key))).ToList();
        if (found.Count > 0)
            ThrowTestFailureReport(AssertFailures.NotContains(Keys, expected, found), base.Current, expected);
        return this;
    }

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> NotContainsSameKeys(IEnumerable expected)
        => NotContainsSameKeys([.. Enumerable.Cast<TKey>(expected)]);

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> ContainsSameKeys(params TKey[] expected)
    {
        CheckNotNull();
        var notFound = expected.Where(key => !Keys.Any(k => IsSame(k, key))).ToList();
        if (notFound.Count > 0)
            ThrowTestFailureReport(AssertFailures.Contains(Keys, expected, notFound), base.Current, expected);
        return this;
    }

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> ContainsSameKeys(IEnumerable expected)
        => ContainsSameKeys([.. Enumerable.Cast<TKey>(expected)]);

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> ContainsSameKeyValue(TKey key, TValue value)
    {
        CheckNotNull();
        var hasKey = Keys.Any(k => IsSame(k, key));
        var expectedKeyValue = new Dictionary<TKey, TValue> { { key, value } };
        if (!hasKey)
            ThrowTestFailureReport(AssertFailures.ContainsKeyValue(expectedKeyValue), base.Current, expectedKeyValue);

        var currentValue = TryGetValue(key);
        if (!IsSame(currentValue, value))
            ThrowTestFailureReport(AssertFailures.ContainsKeyValue(expectedKeyValue, currentValue), base.Current, expectedKeyValue);
        return this;
    }

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> IsSame(IEnumerable expected)
    {
        if (!ReferenceEquals(base.Current, expected))
            ThrowTestFailureReport(AssertFailures.IsSame(base.Current, expected), base.Current, expected);
        return this;
    }

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> IsSame(IDictionary<TKey, TValue> expected)
    {
        if (!ReferenceEquals(base.Current, expected))
            ThrowTestFailureReport(AssertFailures.IsSame(base.Current, expected), base.Current, expected);
        return this;
    }

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> IsNotSame(IEnumerable expected)
    {
        if (ReferenceEquals(base.Current, expected))
            ThrowTestFailureReport(AssertFailures.IsSame(base.Current, expected), base.Current, expected);
        return this;
    }

    /// <inheritdoc/>
    public IDictionaryConstraint<TKey, TValue> IsNotSame(IDictionary<TKey, TValue> expected)
    {
        if (ReferenceEquals(base.Current, expected))
            ThrowTestFailureReport(AssertFailures.IsSame(base.Current, expected), base.Current, expected);
        return this;
    }

    internal static DictionaryAssert<TKey, TValue> From(IDictionary? current)
        => new(current);

    private ICollection<TKey>? GetKeys()
    {
        if (IsGeneric)
            return CurrentTyped?.Keys;

        if (Current?.Keys is not { } keys)
            return [];

        return [.. Enumerable.Cast<TKey>(keys)];
    }

    private TValue? TryGetValue(TKey key)
    {
        if (Current != null)
            return (TValue)Current[key]!;
        return CurrentTyped?.ContainsKey(key) == true ? CurrentTyped[key] : default;
    }

    private void CheckNotNull()
    {
        if (base.Current == null)
            ThrowTestFailureReport(AssertFailures.IsNotNull(), base.Current, null);
    }
}
