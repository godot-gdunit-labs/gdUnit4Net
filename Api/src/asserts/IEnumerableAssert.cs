// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Asserts;

using Constraints;

/// <summary>
///     An Assertion tool to verify enumerating.
/// </summary>
/// <typeparam name="TValue">The type of elements in the enumerable being asserted.</typeparam>
public interface IEnumerableAssert<in TValue> : IEnumerableConstraint<TValue>, IAssertMessage<IEnumerableConstraint<TValue>>
{
}
