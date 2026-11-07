// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Asserts;

using Constraints;

/// <summary>
///     An Assertion to verify boolean values.
/// </summary>
public interface IBoolAssert : IBoolConstraint, IAssertMessage<IBoolConstraint>
{
}
