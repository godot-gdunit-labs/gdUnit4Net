// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Extensions;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Godot;
using Godot.Collections;

using Array = Godot.Collections.Array;

/// <summary>
///     A extension box/unbox Godot Variants.
/// </summary>
internal static class GodotVariantExtensions
{
    public static bool IsGenericGodotDictionary(this Type type) => type
        .GetInterfaces()
        .Any(interfaceType => interfaceType.FullName == "Godot.Collections.IGenericGodotDictionary");

    public static dynamic? UnboxVariant<T>(this T? value)
    {
        if (value == null)
            return null;
        if (value is Variant v)
        {
            if (v.VariantType != Variant.Type.Object)
                return v.UnboxVariant();

            if (value is GodotObject godotObject && !GodotObject.IsInstanceValid(godotObject))
                return godotObject;

            return v.UnboxVariant();
        }

        if (value is StringName sn)
            return sn.ToString();
        if (value is Dictionary gd)
            return gd.UnboxVariant();
        if (value is Array ga)
            return ga.UnboxVariant();
        if (value.GetType().IsGenericGodotDictionary() && value is IEnumerable godotDict)
            return godotDict.UnboxGenericGodotDictionary();
        if (value is Variant[] parameters)
            return parameters.UnboxVariant();
        return value;
    }

    internal static Variant ToVariant(this GodotObject? obj)
        => Variant.From(obj);

    internal static Variant ToVariant(this object? obj) => obj switch
    {
        Dictionary v => v,
        Godot.Collections.Dictionary<Variant, Variant> v => v,
        Array v => v,
        Array<Variant> v => v,
        null => default,
        _ => Type.GetTypeCode(obj.GetType()) switch
        {
            TypeCode.Empty => default,
            TypeCode.String => Variant.CreateFrom((string)obj),
            TypeCode.Boolean => Variant.CreateFrom((bool)obj),
            TypeCode.Char => Variant.CreateFrom(0 + (char)obj),
            TypeCode.SByte => Variant.CreateFrom((sbyte)obj),
            TypeCode.Byte => Variant.CreateFrom((byte)obj),
            TypeCode.Int16 => Variant.CreateFrom((short)obj),
            TypeCode.UInt16 => Variant.CreateFrom((ushort)obj),
            TypeCode.Int32 => Variant.CreateFrom((int)obj),
            TypeCode.UInt32 => Variant.CreateFrom((uint)obj),
            TypeCode.Int64 => Variant.CreateFrom((long)obj),
            TypeCode.UInt64 => Variant.CreateFrom((ulong)obj),
            TypeCode.Single => Variant.CreateFrom((float)obj),
            TypeCode.Double => Variant.CreateFrom((double)obj),
            TypeCode.Decimal => Variant.CreateFrom((double)(decimal)obj), // Godot has no decimal type, its float is a double
            TypeCode.Object => ToVariantByType(obj),
            TypeCode.DBNull => ToVariantByType(obj),
            TypeCode.DateTime => ToVariantByType(obj),
            _ => ToVariantByType(obj)
        }
    };

    private static IDictionary UnboxGenericGodotDictionary(this IEnumerable value)
    {
        var type = value.GetType();
        if (!type.IsGenericGodotDictionary())
            throw new InvalidOperationException($"The given value {nameof(value)} is not Godot.Collections.IGenericGodotDictionary!");
        try
        {
            var dictionaryTypeArgs = type.GetGenericArguments();
            var keyType = dictionaryTypeArgs[0];
            var valueType = dictionaryTypeArgs[1];

            // Get the key and value properties of KeyValuePair using reflection
            var typedDictionary = (IDictionary?)Activator.CreateInstance(typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(keyType, valueType));
            ArgumentNullException.ThrowIfNull(typedDictionary);
            var keyValuePairType = typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType);
            var keyProperty = keyValuePairType.GetProperty("Key");
            var valueProperty = keyValuePairType.GetProperty("Value");

            foreach (var entryObj in value)
            {
                if (entryObj.GetType() == keyValuePairType)
                {
                    var k = keyProperty!.GetValue(entryObj);
                    var v = valueProperty!.GetValue(entryObj);
                    typedDictionary.Add(k!, v);
                }
            }

            return typedDictionary;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"The given value {nameof(value)} can't be unboxed to IDictionary!", e);
        }
    }

    private static System.Collections.Generic.Dictionary<object, object?> UnboxVariant(this Dictionary dict)
    {
        var unboxed = new System.Collections.Generic.Dictionary<object, object?>();
        foreach (var kvp in dict)
            unboxed.Add(kvp.Key.UnboxVariant(), kvp.Value.UnboxVariant());
        return unboxed;
    }

    private static List<object?> UnboxVariant(this Array values)
    {
        var unboxed = new List<object?>();
        foreach (var value in values)
            unboxed.Add(value.UnboxVariant());
        return unboxed;
    }

    private static List<object?> UnboxVariant(this Variant[] values)
    {
        var unboxed = new List<object?>();
        foreach (var value in values)
            unboxed.Add(value.UnboxVariant());
        return unboxed;
    }

    private static Variant ToVariantByType(object obj)
    {
        try
        {
            if (obj is Variant v)
                return v;
            if (obj is IList list)
                return list.ToGodotArray();

            if (obj is IDictionary<string, object> dict)
                return dict.ToGodotTypedDictionary();

            return Variant.From(obj);
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"Cannot convert '{obj.GetType()}' to Variant!");
        }
    }

    private static dynamic? UnboxVariant(this Variant v) => v.VariantType switch
    {
        Variant.Type.Nil => null,
        Variant.Type.Bool => v.AsBool(),
        Variant.Type.Int => v.AsInt32(),
        Variant.Type.Float => v.AsSingle(),
        Variant.Type.String => v.AsString(),
        Variant.Type.Vector2 => v.AsVector2(),
        Variant.Type.Vector2I => v.AsVector2I(),
        Variant.Type.Rect2 => v.AsRect2(),
        Variant.Type.Rect2I => v.AsRect2I(),
        Variant.Type.Vector3 => v.AsVector3(),
        Variant.Type.Vector3I => v.AsVector3I(),
        Variant.Type.Transform2D => v.AsTransform2D(),
        Variant.Type.Vector4 => v.AsVector4(),
        Variant.Type.Vector4I => v.AsVector4I(),
        Variant.Type.Plane => v.AsPlane(),
        Variant.Type.Quaternion => v.AsQuaternion(),
        Variant.Type.Aabb => v.AsAabb(),
        Variant.Type.Basis => v.AsBasis(),
        Variant.Type.Transform3D => v.AsTransform3D(),
        Variant.Type.Projection => v.AsProjection(),
        Variant.Type.Color => v.AsColor(),
        Variant.Type.StringName => v.AsStringName(),
        Variant.Type.NodePath => v.AsNodePath(),
        Variant.Type.Rid => v.AsRid(),
        Variant.Type.Object => v.AsGodotObject(),
        Variant.Type.Callable => v.AsCallable(),
        Variant.Type.Signal => v.AsSignal(),
        Variant.Type.Dictionary => v.AsGodotDictionary(),
        Variant.Type.Array => v.AsGodotArray(),
        Variant.Type.PackedByteArray => v.AsByteArray(),
        Variant.Type.PackedInt32Array => v.AsInt32Array(),
        Variant.Type.PackedInt64Array => v.AsInt64Array(),
        Variant.Type.PackedFloat32Array => v.AsFloat32Array(),
        Variant.Type.PackedFloat64Array => v.AsFloat64Array(),
        Variant.Type.PackedStringArray => v.AsStringArray(),
        Variant.Type.PackedVector2Array => v.AsVector2Array(),
        Variant.Type.PackedVector3Array => v.AsVector3Array(),
        Variant.Type.PackedVector4Array => v.AsVector4Array(),
        Variant.Type.PackedColorArray => v.AsColorArray(),
        Variant.Type.Max => throw new NotImplementedException(),
        _ => throw new NotImplementedException($"The UnboxVariant for {nameof(v)} is not implemented!")
    };
}
