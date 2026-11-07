// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Extensions;

using SystemVector2 = System.Numerics.Vector2;
using SystemVector3 = System.Numerics.Vector3;
using SystemVector4 = System.Numerics.Vector4;

internal static class SystemVectorExtension
{
    internal static bool IsEqualApprox(this SystemVector2 vector, SystemVector2 other, SystemVector2 approx)
    {
        var min = other - approx;
        var max = other + approx;

        var r1 = vector.X >= min.X && vector.Y >= min.Y;
        var r2 = vector.X <= max.X && vector.Y <= max.Y;
        return r1 && r2;
    }

    internal static bool IsEqualApprox(this SystemVector3 vector, SystemVector3 other, SystemVector3 approx)
    {
        var min = other - approx;
        var max = other + approx;

        var r1 = vector.X >= min.X && vector.Y >= min.Y && vector.Z >= min.Z;
        var r2 = vector.X <= max.X && vector.Y <= max.Y && vector.Z <= max.Z;
        return r1 && r2;
    }

    internal static bool IsEqualApprox(this SystemVector4 vector, SystemVector4 other, SystemVector4 approx)
    {
        var min = other - approx;
        var max = other + approx;

        var r1 = vector.X >= min.X && vector.Y >= min.Y && vector.Z >= min.Z && vector.W >= min.W;
        var r2 = vector.X <= max.X && vector.Y <= max.Y && vector.Z <= max.Z && vector.W <= max.W;
        return r1 && r2;
    }
}
