// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Asserts;

using Constraints;

/// <summary>
///     An Assertion Tool to verify object values.
/// </summary>
/// <typeparam name="TValue">
///     The object type being tested.
/// </typeparam>
public interface IObjectAssert<TValue> : IObjectConstraint<TValue>, IAssertMessage<IObjectConstraint<TValue>>
{
}
