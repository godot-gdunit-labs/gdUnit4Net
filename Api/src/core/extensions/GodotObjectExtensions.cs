// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Extensions;

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Godot;
using Godot.Collections;

using Array = Godot.Collections.Array;

/// <summary>
///     An extension to compare C# Objects and Godot Objects by unboxing Variants.
/// </summary>
internal static class GodotObjectExtensions
{
    public enum Mode
    {
        CaseSensitive,
        CaseInsensitive
    }

    internal static SceneTree Instance =>
        Engine.GetMainLoop() as SceneTree ?? throw new InvalidOperationException("SceneTree is not initialized");

    /// <summary>
    ///     Gets a utility to synchronize the current thread with the Godot physics thread.
    ///     This can be used to await the completion of a single physics frame in Godot.
    /// </summary>
    internal static SignalAwaiter SyncProcessFrame =>
        Instance.ToSignal(Instance, SceneTree.SignalName.ProcessFrame);

    /// <summary>
    ///     Gets a util to synchronize the current thread with the Godot physics thread.
    /// </summary>
    internal static SignalAwaiter SyncPhysicsFrame =>
        Instance.ToSignal(Instance, SceneTree.SignalName.PhysicsFrame);

    [SuppressMessage(
        "Usage",
        "CA1508:Avoid dead conditional code",
        Justification = "UnboxVariant() can return strings - static analysis limitation")]
    internal static bool VariantEquals<T>(this T? inLeft, T? inRight, Mode compareMode = Mode.CaseSensitive)
    {
        object? left = inLeft.UnboxVariant();
        object? right = inRight.UnboxVariant();
        if (left == null && right == null)
            return true;

        if (left == null || right == null)
            return false;

        if (ReferenceEquals(left, right))
            return true;

        var type = left.GetType();

        if (left is string ls && right is string rs)
            return string.Equals(ls, rs, compareMode == Mode.CaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        if (type.IsPrimitive || left is IEquatable<T>)
            return left.Equals(right);

        return DeepEquals(left, right, compareMode);
    }

    internal static bool DictionaryEquals(this IDictionary left, IDictionary right, Mode compareMode)
    {
        if (left.Count != right.Count)
            return false;

        return left.Keys.Cast<object>()
            .All(key => right.Contains(key) && left[key].VariantEquals(right[key], compareMode));
    }

    internal static bool TypedDictionaryEquals(object left, object right, Mode compareMode)
    {
        var type1 = left.GetType();
        var type2 = right.GetType();
        var count1 = (int)type1.GetProperty("Count")!.GetValue(left)!;
        var count2 = (int)type2.GetProperty("Count")!.GetValue(right)!;

        if (count1 != count2)
            return false;

        var keys = (IEnumerable)type1.GetProperty("Keys")!.GetValue(left)!;
        var leftValues = type1.GetProperty("Item");
        var rightValues = type2.GetProperty("Item");

        if (leftValues == null || rightValues == null)
            return false;

        foreach (var key in keys)
        {
            var leftValue = leftValues.GetValue(left, [key]);
            var rightValue = rightValues.GetValue(right, [key]);

            if (!leftValue.VariantEquals(rightValue, compareMode))
                return false;
        }

        return true;
    }

    internal static Array ToGodotArray(this object[] args)
        => ToGodotArray((IEnumerable)args);

    internal static Array ToGodotArray(this IEnumerable elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        var converted = new Array();
        foreach (var item in elements)
        {
            try
            {
                converted.Add(item.ToVariant());
            }

            // the conversion throws InvalidOperationException for a type without a Variant representation
            catch (InvalidOperationException e)
            {
                Console.WriteLine($"Can't convert {item} to Variant\n {e.StackTrace}");
                converted.Add(Variant.CreateFrom("n.a"));
            }
        }

        return converted;
    }

    internal static Array ToGodotArray(this IEnumerable<object> elements)
        => ToGodotArray((IEnumerable)elements);

    internal static Array<TVariant> ToGodotArray<[MustBeVariant] TVariant>(this IEnumerable<TVariant> elements)
        where TVariant : notnull
        => [.. elements];

    internal static Array<TVariant> ToGodotArray<[MustBeVariant] TVariant>(this TVariant[] args)
        where TVariant : notnull
        => [.. args];

    internal static Dictionary<Variant, Variant> ToGodotTypedDictionary<[MustBeVariant] TKey, [MustBeVariant] TValue>(this IDictionary<TKey, TValue> dict)
        where TKey : notnull
        where TValue : notnull
    {
        var converted = new Dictionary<Variant, Variant>();
        foreach (var (key, value) in dict)
            converted[key.ToVariant()] = value.ToVariant();
        return converted;
    }

    internal static Dictionary ToGodotDictionary(this IDictionary dict)
    {
        var converted = new Dictionary();
        foreach (var key in dict.Keys)
            converted[key.ToVariant()] = dict[key].ToVariant();
        return converted;
    }

    internal static async Task<object?> Invoke(object instance, string methodName, params Variant[] args)
    {
        // if the instance has a GDScript attached, we have to use the Godot `Call` method
        if (instance is GodotObject goi && goi.GetScript().UnboxVariant() is GDScript)
        {
            if (!goi.HasMethod(methodName))
                throw new MissingMethodException($"The method '{methodName}' not exist on this instance.");
            var current = goi.Call(methodName, args);
            if (current.VariantType == Variant.Type.Object && current.As<GodotObject>().GetClass() == "GDScriptFunctionState")
            {
                var results = await goi.ToSignal(current.As<GodotObject>(), "completed");
                return results[0].UnboxVariant();
            }

            return current;
        }

        // for C# implementations we use Invoke
        var mi = instance.GetType().GetMethod(methodName)
                 ?? throw new MissingMethodException($"The method '{methodName}' not exist on this instance.");
        object?[] parameters = args.Length == 0
            ? System.Array.Empty<object>()
            : args.UnboxVariant()?.ToArray() ?? System.Array.Empty<object>();
        object? result;
        var parameterInfo = mi.GetParameters();
        if (!mi.IsStatic)
        {
            result = parameterInfo.Length == 0
                ? mi.Invoke(instance, null)
                : mi.Invoke(instance, parameters);
        }
        else
        {
            result = parameterInfo.Length == 0
                ? mi.Invoke(null, null)
                : mi.Invoke(null, parameters);
        }

        if (result is Task task)
        {
            await task.ConfigureAwait(false);
            var resultProperty = task.GetType().GetProperty("Result")
                                 ?? throw new InvalidOperationException("Task does not have a 'Result' property.");
            return resultProperty.GetValue(task);
        }

        return result;
    }

    internal static bool DeepEquals<T>(T? left, T? right, Mode compareMode = Mode.CaseSensitive)
        => CompareByReflectionInternal(left, right, compareMode, []);

    private static bool CompareByReflectionInternal(object? obj1, object? obj2, Mode compareMode, HashSet<object> visited)
    {
        // Handle null cases
        if (ReferenceEquals(obj1, obj2))
            return true;
        if (obj1 == null || obj2 == null)
            return false;

        // Prevent infinite recursion
        if (visited.Contains(obj1))
            return true;

        _ = visited.Add(obj1);

        // ReSharper disable once NullableWarningSuppressionIsUsed
        if (obj1 is Variant)
            obj1 = obj1.UnboxVariant()!;

        // ReSharper disable once NullableWarningSuppressionIsUsed
        if (obj2 is Variant)
            obj2 = obj2.UnboxVariant()!;

        var type1 = obj1.GetType();
        var type2 = obj2.GetType();

        if (type1 != type2)
            return false;

        // Handle value types and strings
        if (type1 == typeof(string))
            return string.Equals(obj1.ToString(), obj2.ToString(), compareMode == Mode.CaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

        if (type1.IsPrimitive || IsIEquatable(type1))
            return obj1.Equals(obj2);

        if (IsIEqualityComparer(type1))
        {
            var equalsMethod = obj1.GetType().GetMethod("Equals", [type1, type1]);
            var result = equalsMethod?.Invoke(obj1, [obj1, obj2]) ?? false;
            return result is bool b && b;
        }

        // Handle dictionary comparison first
        if (obj1 is IDictionary dict1 && obj2 is IDictionary dict2)
            return DictionaryEquals(dict1, dict2, compareMode);

        if (IsGenericDictionary(obj1) && IsGenericDictionary(obj2))
            return TypedDictionaryEquals(obj1, obj2, compareMode);

        // Handle collections
        if (obj1 is IEnumerable enum1 && obj2 is IEnumerable enum2)
            return CompareEnumerable(enum1, enum2, compareMode, visited);

        // Compare all fields
        var fields = type1.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(field =>
                !field.Name.Equals("NativePtr", StringComparison.Ordinal)
                && !field.Name.Contains("k__BackingField", StringComparison.Ordinal)
                && !field.Name.ToLower().Contains("weakreference", StringComparison.Ordinal));

        foreach (var field in fields)
        {
            var value1 = field.GetValue(obj1);
            var value2 = field.GetValue(obj2);

            if (!CompareByReflectionInternal(value1, value2, compareMode, visited))
                return false;
        }

        // Compare all properties
        var properties = type1.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(p => p.Name != "NativeInstance");
        foreach (var property in properties)
        {
            // an indexer needs arguments to be read, it is no state of the compared object
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;

            object? value1;
            object? value2;
            try
            {
                value1 = property.GetValue(obj1);
                value2 = property.GetValue(obj2);
            }
            catch (Exception e) when (e is TargetInvocationException or NotSupportedException)
            {
                // skip a property whose getter throws, or whose value cannot be boxed (a ref struct type or a ref return)
                continue;
            }

            if (!CompareByReflectionInternal(value1, value2, compareMode, visited))
                return false;
        }

        return true;
    }

    private static bool CompareEnumerable(IEnumerable enum1, IEnumerable enum2, Mode compareMode, HashSet<object> visited)
    {
        var list1 = enum1.Cast<object>().ToList();
        var list2 = enum2.Cast<object>().ToList();

        if (list1.Count != list2.Count)
            return false;

        for (var i = 0; i < list1.Count; i++)
        {
            if (!CompareByReflectionInternal(list1[i], list2[i], compareMode, visited))
                return false;
        }

        return true;
    }

    private static bool IsIEquatable(Type type)
        => type
            .GetInterfaces()
            .Any(i =>
                i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IEquatable<>));

    private static bool IsIEqualityComparer(Type type)
        => type
            .GetInterfaces()
            .Any(i =>
                i.IsGenericType
                && i.GetGenericTypeDefinition() == typeof(IEqualityComparer<>));

    private static bool IsGenericDictionary(object obj)
        => obj.GetType()
            .GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
}
