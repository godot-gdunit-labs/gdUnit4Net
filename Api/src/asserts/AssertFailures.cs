// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Asserts;

using System.Collections;
using System.Runtime.CompilerServices;

using Core.Extensions;

using Godot;
using Godot.Collections;

internal static class AssertFailures
{
    internal const string WARN_COLOR = "#EFF883";
    private const string ERROR_COLOR = "#CD5C5C";
    private const string VALUE_COLOR = "#1E90FF";

    public static string IsEqual(object? current, object? expected) =>
        current is IEnumerable || expected is IEnumerable
            ? $"""
               {FormatFailure("Expecting be equal:")}
               {FormatExpected(expected).Indentation(1)}
                but is
               {FormatCurrent(current).Indentation(1)}
               """
            : $"""
               {FormatFailure("Expecting be equal:")}
               {FormatExpected(expected).Indentation(1)} but is {FormatCurrent(current)}
               """;

    public static string IsEqual(IEnumerable current, IEnumerable expected) =>
        $"""
         {FormatFailure("Expecting be equal:")}
         {FormatExpected(expected).Indentation(1)}
           but is
         {FormatCurrent(current).Indentation(1)}
         """;

    public static string IsEqualIgnoringCase(object? current, object expected) =>
        $"""
         {FormatFailure("Expecting be equal (ignoring case):")}
         {FormatExpected(expected).Indentation(1)}
          but is
         {FormatCurrent(current).Indentation(1)}
         """;

    public static string IsNotEqual(object? current, object? expected) =>
        current is IEnumerable || expected is IEnumerable
            ? $"""
               {FormatFailure("Expecting be NOT equal:")}
               {FormatExpected(expected).Indentation(1)}
                but is
               {FormatCurrent(current).Indentation(1)}
               """
            : $"""
               {FormatFailure("Expecting be NOT equal:")}
               {FormatExpected(expected).Indentation(1)} but is {FormatCurrent(current)}
               """;

    public static string IsNotEqual(IEnumerable current, IEnumerable expected) =>
        $"""
         {FormatFailure("Expecting be NOT equal:")}
         {FormatExpected(expected).Indentation(1)}
          but is
         {FormatCurrent(current).Indentation(1)}
         """;

    public static string IsNotEqualIgnoringCase(object? current, object expected) =>
        $"""
         {FormatFailure("Expecting be NOT equal (ignoring case):")}
         {FormatExpected(expected).Indentation(1)}
          but is
         {FormatCurrent(current).Indentation(1)}
         """;

    public static string IsNull(object current) =>
        $"""
         {FormatFailure("Expecting be <Null>:")}
          but is
         {FormatCurrent(current).Indentation(1)}
         """;

    public static string IsNotNull() => "Expecting be NOT <Null>:";

    public static string IsEmpty(int size, bool isNull) =>
        isNull
            ? $"""
               {FormatFailure("Expecting be empty:")}
                but is <Null>
               """
            : $"""
               {FormatFailure("Expecting be empty:")}
                but has size {FormatCurrent(size)}
               """;

    public static string IsEmpty(string? current) =>
        $"""
         {FormatFailure("Expecting be empty:")}
          but is
         {FormatCurrent(current).Indentation(1)}
         """;

    public static string IsNotEmpty() =>
        $"""
         {FormatFailure("Expecting being NOT empty:")}
          but is empty
         """;

    public static string NotInstanceOf(Type? expected) =>
        $"""
         {FormatFailure("Expecting be NOT a instance of:")}
         {FormatExpected(expected).Indentation(1)}
         """;

    public static string IsInstanceOf(Type? current, Type expected) =>
        $"""
         {FormatFailure("Expected be instance of:")}
         {FormatExpected(expected).Indentation(1)} but is {FormatCurrent(current)}
         """;

    public static string IsSame<TValue>(TValue? current, TValue expected) =>
        $"""
         {FormatFailure("Expecting be same:")}
         {FormatExpected(expected).Indentation(1)}
          to refer to the same object
         {FormatCurrent(current).Indentation(1)}
         """;

    public static string IsNotSame<TValue>(TValue? expected) =>
        $"{FormatFailure("Expecting be NOT same:")} {FormatExpected(expected)}";

    public static string IsBetween<TValue>(TValue? current, TValue from, TValue to) =>
        $"""
         {FormatFailure("Expecting:")}
         {FormatCurrent(current).Indentation(1)}
          in range between
         {FormatExpected(from).Indentation(1)} <> {FormatExpected(to)}
         """;

    public static string IsNotBetween(object? current, object from, object to) =>
        $"""
         {FormatFailure("Expecting:")}
         {FormatCurrent(current).Indentation(1)}
          be NOT in range between
         {FormatExpected(from).Indentation(1)} <> {FormatExpected(to)}
         """;

    public static string IsEven(object? current) =>
        $"""
         {FormatFailure("Expecting be even:")}
          but is {FormatCurrent(current)}
         """;

    public static string IsOdd(object? current) =>
        $"""
         {FormatFailure("Expecting be odd:")}
          but is {FormatCurrent(current)}
         """;

    public static string HasSize(object? current, object expected) =>
        $"""
         {FormatFailure("Expecting size:")}
         {FormatExpected(expected).Indentation(1)} but is {(current == null ? "unknown" : FormatCurrent(current))}
         """;

    public static string IsGreater(object current, object expected) =>
        $"""
         {FormatFailure("Expecting to be greater than:")}
         {FormatExpected(expected).Indentation(1)} but is {FormatCurrent(current)}
         """;

    public static string IsGreaterEqual(object current, object expected) =>
        $"""
         {FormatFailure("Expecting to be greater than or equal:")}
         {FormatExpected(expected).Indentation(1)} but is {FormatCurrent(current)}
         """;

    public static string IsLess(object current, object expected) =>
        $"""
         {FormatFailure("Expecting to be less than:")}
         {FormatExpected(expected).Indentation(1)} but is {FormatCurrent(current)}
         """;

    public static string IsLessEqual(object current, object expected) =>
        $"""
         {FormatFailure("Expecting to be less than or equal:")}
         {FormatExpected(expected).Indentation(1)} but is {FormatCurrent(current)}
         """;

    public static string IsNegative(object current) =>
        $"""
         {FormatFailure("Expecting be negative:")}
          but is {FormatCurrent(current)}
         """;

    public static string IsNotNegative(object current) =>
        $"""
         {FormatFailure("Expecting be NOT negative:")}
          but is {FormatCurrent(current)}
         """;

    public static string IsNotZero() =>
        $"""
         {FormatFailure("Expecting be NOT zero:")}
          but is '0'
         """;

    public static string IsZero(object? current) =>
        $"""
         {FormatFailure("Expecting be zero:")}
          but is {FormatCurrent(current)}
         """;

    public static string IsIn(object? current, object expected) =>
        $"""
         {FormatFailure("Expecting:")}
         {FormatCurrent(current).Indentation(1)}
          is in
         {FormatExpected(expected).Indentation(1)}
         """;

    public static string IsNotIn(object? current, object expected) =>
        $"""
         {FormatFailure("Expecting:")}
         {FormatCurrent(current).Indentation(1)}
          is not in
         {FormatExpected(expected).Indentation(1)}
         """;

    public static string Contains<TValue>(IEnumerable<TValue?>? current, IEnumerable<TValue?> expected, IEnumerable<TValue?> notFound) =>
        $"""
         {FormatFailure("Expecting contains elements:")}
         {FormatCurrent(current).Indentation(1)}
          do contains (in any order)
         {FormatExpected(expected).Indentation(1)}
          but could not find elements:
         {FormatExpected(notFound).Indentation(1)}
         """;

    public static string ContainsExactly<TValue>(IEnumerable<TValue>? current, IEnumerable<TValue> expected, List<TValue> notFound, List<TValue> notExpected)
    {
        if (notExpected.Count != 0 && notExpected.Count == notFound.Count)
        {
            var diff = notExpected.FindAll(e => !notFound.Any(e2 => Equals(e.UnboxVariant(), e2.UnboxVariant())));
            if (diff.Count == 0)
            {
                return $"""
                        {FormatFailure("Expecting contains exactly elements:")}
                        {FormatCurrent(current).Indentation(1)}
                         do contains (in same order)
                        {FormatExpected(expected).Indentation(1)}
                         but there has differences in order:
                        {ListDifferences([.. notFound], [.. notExpected])}
                        """;
            }
        }

        var message = $"""
                       {FormatFailure("Expecting contains exactly elements:")}
                       {FormatCurrent(current).Indentation(1)}
                        do contains (in same order)
                       {FormatExpected(expected).Indentation(1)}
                       """;
        if (notExpected.Count > 0)
        {
            message += $"""

                         but others where not expected:
                        {FormatExpected(notExpected).Indentation(1)}
                        """;
        }

        if (notFound.Count > 0)
        {
            message += $"""

                         {(notExpected.Count == 0 ? "but" : "and")} some elements not found:
                        {FormatExpected(notFound).Indentation(1)}
                        """;
        }

        return message.UnixFormat();
    }

    public static string ContainsExactlyInAnyOrder<TValue>(IEnumerable<TValue>? current, IEnumerable<TValue> expected, List<TValue> notFound, List<TValue> notExpected)
    {
        var message = $"""
                       {FormatFailure("Expecting contains exactly elements:")}
                       {FormatCurrent(current).Indentation(1)}
                        do contains (in any order)
                       {FormatExpected(expected).Indentation(1)}
                       """;
        if (notExpected.Count > 0)
        {
            message += $"""

                         but some elements where not expected:
                        {FormatExpected(notExpected).Indentation(1)}
                        """;
        }

        if (notFound.Count > 0)
        {
            message += $"""

                         {(notExpected.Count == 0 ? "but" : "and")} could not find elements:
                        {FormatExpected(notFound).Indentation(1)}
                        """;
        }

        return message;
    }

    public static string ContainsKeyValue(IDictionary expected) =>
        $"""
         {FormatFailure("Expecting do contain entry:")}
         {FormatCurrent(expected).Indentation(1)}
         """;

    public static string ContainsKeyValue(IDictionary expected, object? currentValue) =>
        $"""
         {FormatFailure("Expecting do contain entry:")}
         {FormatCurrent(expected).Indentation(1)}
          found key but value is
         {FormatCurrent(currentValue).Indentation(1)}
         """;

    public static string NotContains(string current, string expected) =>
        $"""
         {FormatFailure("Expecting:")}
         {FormatCurrent(current).Indentation(1)}
          do not contain
         {FormatExpected(expected).Indentation(1)}
         """;

    public static string NotContains<T>(IEnumerable<T>? current, IEnumerable<T> expected, List<T> found) =>
        $"""
         {FormatFailure("Expecting:")}
         {FormatCurrent(current).Indentation(1)}
          do NOT contains (in any order)
         {FormatExpected(expected).Indentation(1)}
          but found elements:
         {FormatExpected(found).Indentation(1)}
         """;

    public static string NotContainsIgnoringCase(string current, string expected) =>
        $"""
         {FormatFailure("Expecting:")}
         {FormatCurrent(current).Indentation(1)}
          do not contain (ignoring case)
         {FormatExpected(expected).Indentation(1)}
         """;

    public static string HasValue(string methodName, object? current, object expected) =>
        $"""
         {FormatFailure("Expecting Property:")}
         {FormatCurrent(methodName).Indentation(1)} to be {FormatCurrent(expected)} but is {FormatCurrent(current)}
         """;

    public static string Contains(string? current, string expected) =>
        $"""
         {FormatFailure("Expecting:")}
         {FormatCurrent(current).Indentation(1)}
          do contains
         {FormatExpected(expected).Indentation(1)}
         """;

    public static string ContainsIgnoringCase(string? current, string expected) =>
        $"""
         {FormatFailure("Expecting:")}
         {FormatCurrent(current).Indentation(1)}
          do contains (ignoring case)
         {FormatExpected(expected).Indentation(1)}
         """;

    public static string EndsWith(string? current, string expected) =>
        $"""
         {FormatFailure("Expecting:")}
         {FormatCurrent(current).Indentation(1)}
          to end with
         {FormatExpected(expected).Indentation(1)}
         """;

    public static string StartsWith(string? current, string expected) =>
        $"""
         {FormatFailure("Expecting:")}
         {FormatCurrent(current).Indentation(1)}
          to start with
         {FormatExpected(expected).Indentation(1)}
         """;

    public static string HasLength(int currentLength, int expectedLength, IStringAssert.Compare comparator)
    {
        var errorMessage = comparator switch
        {
            IStringAssert.Compare.EQUAL => "Expecting length:",
            IStringAssert.Compare.LESS_THAN => "Expecting length to be less than:",
            IStringAssert.Compare.LESS_EQUAL => "Expecting length to be less than or equal:",
            IStringAssert.Compare.GREATER_THAN => "Expecting length to be greater than:",
            IStringAssert.Compare.GREATER_EQUAL => "Expecting length to be greater than or equal:",
            _ => "Invalid comparator"
        };
        return $"""
                {FormatFailure(errorMessage)}
                {FormatExpected(expectedLength).Indentation(1)} but is {(currentLength == -1 ? "unknown" : FormatCurrent(currentLength))}
                """;
    }

    internal static string AsObjectId(object? value)
    {
        if (value == null)
            return "<Null>";

        var unboxedValue = value;
        var type = value.GetType();
        if (value is Variant gv)
        {
            unboxedValue = gv.UnboxVariant();
            if (unboxedValue == null)
                return "<Godot.Variant> (Null)";
            type = unboxedValue.GetType();
        }

        string instanceId;
        var name = $"<{type.FullName?
            .Replace("[", string.Empty, StringComparison.Ordinal)
            .Replace("]", string.Empty, StringComparison.Ordinal) ?? "unknown"}>";
        if (unboxedValue is GodotObject go)
        {
            var id = GodotObject.IsInstanceValid(go) ? go.GetInstanceId().ToString() : "<null>";
            instanceId = $"objId: {id}";
        }
        else
        {
            instanceId = $"objId: {RuntimeHelpers.GetHashCode(value)}";
            if (HasOverriddenToString(value))
                name = value.ToString();
        }

        return $"{name} ({instanceId})";
    }

    internal static string FormatValue(object? value, string color, bool quoted)
    {
        if (value == null)
            return "<Null>";

        if (value is string vs)
            return quoted ? vs.Formatted() : vs;

        if (value is Type)
            return $"[color={color}]<{value}>[/color]";

        if (value is Variant gv)
            value = gv.UnboxVariant();

        if (value is IDictionary dict)
            return FormatDictionary(dict, color);

        if (value is Dictionary gDict)
            return FormatDictionary(gDict, color);

#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var type = value.GetType();
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        if (type.IsGenericGodotDictionary())
            return FormatDictionary(value.UnboxVariant(), color);

        if (value is IEnumerable values)
            return FormatEnumerable(values, color);

        if ((type.IsClass && value is not string) || value is Type)
            return $"[color={color}]{AsObjectId(value)}[/color]";
        return quoted ? $"'[color={color}]{value.Formatted()}[/color]'" : $"[color={color}]{value.Formatted()}[/color]";
    }

    internal static string IsEmitted(object? current, string signal, Variant[] args) =>
        $"""
         {FormatFailure("Expecting do emitting signal:")}
         {FormatExpected($"{signal}({args.Formatted()})").Indentation(1)}
          by
         {FormatCurrent(current).Indentation(1)}
         """;

    internal static string IsNotEmitted(object? current, string signal, Variant[] args) =>
        $"""
         {FormatFailure("Expecting do NOT emitting signal:")}
         {FormatExpected($"{signal}({args.Formatted()})").Indentation(1)}
          by
         {FormatCurrent(current).Indentation(1)}
         """;

    internal static string IsSignalExists(object current, string signal) =>
        $"""
         {FormatFailure("Expecting signal exists:")}
         {FormatExpected($"{signal}()").Indentation(1)}
          on
         {FormatCurrent(current).Indentation(1)}
         """;

    internal static string IsTrue() =>
        $"{FormatFailure("Expecting:")} {FormatExpected(true)} but is {FormatCurrent(false)}";

    internal static string IsFalse() =>
        $"{FormatFailure("Expecting:")} {FormatExpected(false)} but is {FormatCurrent(true)}";

    private static bool HasOverriddenToString(object obj)
    {
        if (obj == null)
            return false;
        if (obj is string)
            return false;

        var toStringMethod = obj.GetType().GetMethod("ToString");
        return toStringMethod?.DeclaringType != typeof(object);
    }

    private static string FormatDictionary(IDictionary dict, string color)
    {
        if (dict.Keys.Count == 0)
            return $"[color={color}]<Empty>[/color]";

        var sortedKeys = dict.Keys.Cast<object>().OrderBy(k => k.ToString());
        var keyValues = sortedKeys.Select(key => $"{{{key.Formatted()}, {dict[key].Formatted()}}}");
        var pairs = string.Join("; ", keyValues);
        return $"[color={color}]{pairs}[/color]";
    }

#pragma warning disable CA1859
    private static string FormatDictionary(IDictionary<Variant, Variant> dict, string color)
    {
#pragma warning restore CA1859
        if (dict.Keys.Count == 0)
            return $"[color={color}]<Empty>[/color]";

        var keyValues = new ArrayList();
        foreach (var entry in dict)
        {
            object? key = entry.Key.UnboxVariant();
            object? value = entry.Value.UnboxVariant();
            _ = keyValues.Add($"{{{key.Formatted()}, {value.Formatted()}}}");
        }

        var pairs = string.Join("; ", keyValues.ToArray());
        return $"[color={color}]{pairs}[/color]";
    }

    private static string FormatEnumerable(IEnumerable enumerable, string color)
    {
        ArgumentNullException.ThrowIfNull(enumerable);
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Color cannot be empty", nameof(color));

        var enumerator = enumerable.GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
                return $"[color={color}]<Empty>[/color]";

            var keyValues = new List<object?>();
            do
                keyValues.Add(enumerator.Current);
            while (enumerator.MoveNext());
            return $"[color={color}]{keyValues.Formatted()}[/color]";
        }
        finally
        {
            (enumerator as IDisposable)?.Dispose();
        }
    }

    private static string ListDifferences<TValue>(List<TValue> left, List<TValue> right)
    {
        var output = new List<string>();
        foreach (var it in left
                     .Select((value, i) => new
                     {
                         Value = value,
                         Index = i
                     }))
        {
            var l = it.Value;
            var r = right.ElementAt(it.Index);
            output.Add($"- element at index {it.Index} expect {FormatCurrent(l.UnboxVariant())} but is {FormatExpected(r.UnboxVariant())}");
        }

        return string.Join("\n", output).Indentation(1);
    }

    private static string FormatCurrent(object? value)
        => FormatValue(value, VALUE_COLOR, true)
            .UnixFormat();

    private static string FormatExpected(object? value)
        => FormatValue(value, VALUE_COLOR, true)
            .UnixFormat();

    private static string FormatFailure(object value)
        => FormatValue(value, ERROR_COLOR, false)
            .UnixFormat();
}
