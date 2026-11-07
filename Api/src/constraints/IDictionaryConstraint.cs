// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Constraints;

using System.Collections;

using Asserts;

/// <summary>
///     A set of constrains to verify dictionary values.
///     Provides specialized assertions for validating dictionaries and their contents.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public interface IDictionaryConstraint<TKey, TValue> : IAssertBase<IEnumerable, IDictionaryConstraint<TKey, TValue>>
    where TKey : notnull
{
    /// <summary>
    ///     Verifies that the current dictionary is empty; it has a size of 0.
    /// </summary>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> IsEmpty();

    /// <summary>
    ///     Verifies that the current dictionary is not empty; it has a size of minimum 1.
    /// </summary>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> IsNotEmpty();

    /// <summary>
    ///     Verifies that the current dictionary has a size of given value.
    /// </summary>
    /// <param name="expected">The expected size.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> HasSize(int expected);

    /// <summary>
    ///     Verifies that the current dictionary contains the given key(s).
    /// </summary>
    /// <param name="expected">The keys to be contained in the dictionary.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> ContainsKeys(params TKey[] expected);

    /// <summary>
    ///     Verifies that the current dictionary contains the given keys.
    /// </summary>
    /// <param name="expected">The keys to be contained in the dictionary.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> ContainsKeys(IEnumerable expected);

    /// <summary>
    ///     Verifies that the current dictionary not contains the given key(s).
    /// </summary>
    /// <param name="expected">Keys to be NOT contained in the dictionary.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> NotContainsKeys(params TKey[] expected);

    /// <summary>
    ///     Verifies that the current dictionary not contains the given keys.
    /// </summary>
    /// <param name="expected">Keys to be NOT contained in the dictionary.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> NotContainsKeys(IEnumerable expected);

    /// <summary>
    ///     Verifies that the current dictionary not contains the given key(s) by object reference.
    /// </summary>
    /// <remarks>
    ///     The key and value are compared by object reference, for deep parameter comparison use <see cref="NotContainsKeys(TKey[])" />.
    /// </remarks>
    /// <param name="expected">Keys to be NOT contained in the dictionary.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> NotContainsSameKeys(params TKey[] expected);

    /// <summary>
    ///     Verifies that the current dictionary not contains the given keys by object reference.
    /// </summary>
    /// <remarks>
    ///     The key and value are compared by object reference, for deep parameter comparison use <see cref="NotContainsKeys(IEnumerable)" />.
    /// </remarks>
    /// <param name="expected">Keys to be NOT contained in the dictionary.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> NotContainsSameKeys(IEnumerable expected);

    /// <summary>
    ///     Verifies that the current dictionary contains the given key and value.
    /// </summary>
    /// <param name="key">The key to be contained.</param>
    /// <param name="value">The value to be contained.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> ContainsKeyValue(TKey key, TValue value);

    /// <summary>
    ///     Verifies that the current dictionary contains the given key(s) by object reference.
    /// </summary>
    /// <remarks>
    ///     The key and value are compared by object reference, for deep parameter comparison use <see cref="ContainsKeys(TKey[])" />.
    /// </remarks>
    /// <param name="expected">The keys to be contained in the dictionary.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> ContainsSameKeys(params TKey[] expected);

    /// <summary>
    ///     Verifies that the current dictionary contains the given keys by object reference.
    /// </summary>
    /// <remarks>
    ///     The keys are compared by object reference, for deep parameter comparison use <see cref="ContainsKeys(IEnumerable)" />.
    /// </remarks>
    /// <param name="expected">The keys to be contained in the dictionary.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> ContainsSameKeys(IEnumerable expected);

    /// <summary>
    ///     Verifies that the current dictionary contains the given key and value by object reference.
    /// </summary>
    /// <param name="key">The key to be contained.</param>
    /// <param name="value">The value to be contained.</param>
    /// <remarks>
    ///     The key and value are compared by object reference, for deep parameter comparison use <see cref="ContainsKeyValue" />.
    /// </remarks>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> ContainsSameKeyValue(TKey key, TValue value);

    /// <summary>
    ///     Verifies that the current dictionary is the same.
    /// </summary>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep parameter comparison use <see cref="IAssertBase{TValue,TAssert}.IsEqual" />.
    /// </remarks>
    /// <param name="expected">The dictionary to be the same.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> IsSame(IDictionary<TKey, TValue> expected);

    /// <summary>
    ///     Verifies that the current dictionary is the same as the given enumerable.
    /// </summary>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep parameter comparison use <see cref="IAssertBase{TValue,TAssert}.IsEqual" />.
    /// </remarks>
    /// <param name="expected">The enumerable to be the same.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> IsSame(IEnumerable expected);

    /// <summary>
    ///     Verifies that the current dictionary is NOT the same.
    /// </summary>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep parameter comparison use <see cref="IAssertBase{TValue,TAssert}.IsNotEqual" />.
    /// </remarks>
    /// <param name="expected">The dictionary to be NOT the same.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> IsNotSame(IDictionary<TKey, TValue> expected);

    /// <summary>
    ///     Verifies that the current dictionary is NOT the same as the given enumerable.
    /// </summary>
    /// <remarks>
    ///     Compares the current by object reference equals, for deep parameter comparison use <see cref="IAssertBase{TValue,TAssert}.IsNotEqual" />.
    /// </remarks>
    /// <param name="expected">The enumerable to be NOT the same.</param>
    /// <returns>IDictionaryAssert for fluent method chaining.</returns>
    IDictionaryConstraint<TKey, TValue> IsNotSame(IEnumerable expected);
}
