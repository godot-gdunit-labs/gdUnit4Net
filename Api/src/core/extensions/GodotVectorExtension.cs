// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Extensions;

using Godot;

internal static class GodotVectorExtension
{
    internal static bool IsEqualApprox(this Vector2 vector, Vector2 other, Vector2 approx)
    {
        var min = other - approx;
        var max = other + approx;

        var r1 = vector.X >= min.X && vector.Y >= min.Y;
        var r2 = vector.X <= max.X && vector.Y <= max.Y;
        return r1 && r2;
    }

    internal static bool IsEqualApprox(this Vector2I vector, Vector2I other, Vector2I approx)
    {
        var min = other - approx;
        var max = other + approx;

        var r1 = vector.X >= min.X && vector.Y >= min.Y;
        var r2 = vector.X <= max.X && vector.Y <= max.Y;
        return r1 && r2;
    }

    internal static bool IsEqualApprox(this Vector3 vector, Vector3 other, Vector3 approx)
    {
        var min = other - approx;
        var max = other + approx;

        var r1 = vector.X >= min.X && vector.Y >= min.Y && vector.Z >= min.Z;
        var r2 = vector.X <= max.X && vector.Y <= max.Y && vector.Z <= max.Z;
        return r1 && r2;
    }

    internal static bool IsEqualApprox(this Vector3I vector, Vector3I other, Vector3I approx)
    {
        var min = other - approx;
        var max = other + approx;

        var r1 = vector.X >= min.X && vector.Y >= min.Y && vector.Z >= min.Z;
        var r2 = vector.X <= max.X && vector.Y <= max.Y && vector.Z <= max.Z;
        return r1 && r2;
    }

    internal static bool IsEqualApprox(this Vector4 vector, Vector4 other, Vector4 approx)
    {
        var min = other - approx;
        var max = other + approx;

        var r1 = vector.X >= min.X && vector.Y >= min.Y && vector.Z >= min.Z && vector.W >= min.W;
        var r2 = vector.X <= max.X && vector.Y <= max.Y && vector.Z <= max.Z && vector.W <= max.W;
        return r1 && r2;
    }

    internal static bool IsEqualApprox(this Vector4I vector, Vector4I other, Vector4I approx)
    {
        var min = other - approx;
        var max = other + approx;

        var r1 = vector.X >= min.X && vector.Y >= min.Y && vector.Z >= min.Z && vector.W >= min.W;
        var r2 = vector.X <= max.X && vector.Y <= max.Y && vector.Z <= max.Z && vector.W <= max.W;
        return r1 && r2;
    }
}
