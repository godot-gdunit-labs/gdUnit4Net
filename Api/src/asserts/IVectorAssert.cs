// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Asserts;

using Constraints;

/// <summary>
///     An assertion tool to verify Godot.Vector values in the GdUnit4 testing framework.
///     Provides specialized methods for comparing and validating vector types from the Godot engine.
/// </summary>
/// <typeparam name="TValue">
///     The vector type being tested. Must implement IEquatable{TValue} to enable value comparisons.
///     Typically used with Godot.Vector2, Godot.Vector3, or Godot.Vector4 types.
/// </typeparam>
public interface IVectorAssert<in TValue> : IVectorConstraint<TValue>, IAssertMessage<IVectorConstraint<TValue>>
    where TValue : IEquatable<TValue>
{
}
