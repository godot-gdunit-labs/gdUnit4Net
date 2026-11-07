// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Asserts;

using Constraints;

/// <summary>
///     An Assertion Tool to verify Godot signals.
/// </summary>
public interface ISignalAssert : ISignalConstraint, IAssertMessage<ISignalConstraint>
{
}
