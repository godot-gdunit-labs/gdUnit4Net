// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Asserts;

using Constraints;

/// <summary>
///     An Assertion Tool to verify dictionary values.
///     Provides specialized assertions for validating dictionaries and their contents.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
public interface IDictionaryAssert<TKey, TValue> : IDictionaryConstraint<TKey, TValue>, IAssertMessage<IDictionaryConstraint<TKey, TValue>>
    where TKey : notnull
{
}
