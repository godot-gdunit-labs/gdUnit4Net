// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Asserts;

using Core.Extensions;

/// <inheritdoc />
public sealed class Tuple : ITuple
{
    private readonly IEnumerable<object?> values;

    /// <summary>
    /// Initializes a new instance of the <see cref="Tuple"/> class.
    /// </summary>
    /// <param name="args">The values holding by the tuple.</param>
    public Tuple(params object?[] args) => values = [.. args];

    /// <inheritdoc/>
    public IEnumerable<object?> Values
    {
        get => values;
        set => throw new NotImplementedException();
    }

    /// <inheritdoc cref="ITuple" />
    public static bool operator ==(Tuple? tuple1, Tuple? tuple2)
    {
        if (ReferenceEquals(tuple1, tuple2))
            return true;
        if (tuple1 is null || tuple2 is null)
            return false;
        return tuple1.Values.VariantEquals(tuple2.Values);
    }

    /// <inheritdoc cref="ITuple" />
    public static bool operator !=(Tuple? tuple1, Tuple? tuple2) => !(tuple1 == tuple2);

    /// <inheritdoc cref="ITuple" />
    public override bool Equals(object? obj) => obj is Tuple tuple && Values.VariantEquals(tuple.Values);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(values);

    /// <inheritdoc/>
    public override string ToString() => $"tuple({string.Join(", ", Values.Select(GdUnitExtensions.Formatted)).Indentation(0)})";
}
