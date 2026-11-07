// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text
namespace GdUnit4.Asserts;

using System.Numerics;

using Constraints;

/// <summary>
///     Base interface for numeric value assertions in the GdUnit4 testing framework.
///     Provides specialized methods for comparing and validating numeric values.
/// </summary>
/// <typeparam name="TValue">
///     The numeric type being tested. Must implement IComparable, IComparable{TValue}, IEquatable{TValue},
///     IAdditionOperators{TValue, TValue, TValue}, and ISubtractionOperators{TValue, TValue, TValue}.
/// </typeparam>
public interface INumberAssert<in TValue> : INumberConstraint<TValue>, IAssertMessage<INumberConstraint<TValue>>
    where TValue : IComparable, IComparable<TValue>, IEquatable<TValue>,
    IAdditionOperators<TValue, TValue, TValue>, ISubtractionOperators<TValue, TValue, TValue>
{
}
