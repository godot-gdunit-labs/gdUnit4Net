// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Extensions;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using Asserts;

using Godot;
using Godot.Collections;

using Array = Godot.Collections.Array;
using Tuple = Asserts.Tuple;

/// <summary>
///     A util extension to format a Godot object into a string representation.
/// </summary>
internal static partial class GdUnitExtensions
{
    internal static string ToSnakeCase(this string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Use regular expressions to match and replace camel case patterns
        return RegexToSnakeCase().Replace(input, "$1_$2").ToLower();
    }

    internal static string Formatted(this object? value)
    {
        if (value is Variant v)
            value = v.UnboxVariant();
        if (value == null)
            return "<Null>";
        if (value is string asString)
            return asString.Formatted();
        return value switch
        {
            IEnumerable en => en.Formatted(),
            Tuple tuple => tuple.ToString(),
            _ => FormatAsValueOrClass(value)
        };
    }

    internal static string Formatted(this Variant value)
        => Formatted(value as object);

    internal static string Formatted(this Tuple? value)
        => value?.ToString() ?? "<Null>";

    internal static string Formatted(this Array args, int indentation = 0)
        => args.ToArray().Formatted(indentation);

    internal static string Formatted<[MustBeVariant] TValue>(this Array<TValue> args, int indentation = 0)
        => args.ToArray().Formatted(indentation);

    internal static string UnixFormat(this string value) => value.Replace("\r", string.Empty, StringComparison.OrdinalIgnoreCase);

    internal static string Indentation(this string value, int indentation)
    {
        if (indentation == 0 || string.IsNullOrEmpty(value))
            return value;
        var indent = new string(' ', indentation * 4);
        var lines = value.UnixFormat().Split("\n");
        return string.Join("\n", lines.Select(line => indent + line));
    }

    internal static string Humanize(this TimeSpan t)
    {
        var parts = new List<string>();
        if (t.Hours > 1)
            parts.Add($@"{t:%h}h");
        if (t.Minutes > 0)
            parts.Add($@"{t:%m}min");
        if (t.Seconds > 0)
            parts.Add($@"{t:%s}s");
        if (t.Milliseconds > 0)
            parts.Add($@"{t:fff}ms");
        return string.Join(" ", parts);
    }

    internal static string RichTextNormalize(this string? input) => RegexTextNormalize().Replace(input?.UnixFormat() ?? string.Empty, string.Empty);

    internal static int GetWithTimeoutLineNumber()
    {
        var saveStackTrace = new StackTrace(true);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        return saveStackTrace.FrameCount > 4 ? saveStackTrace.GetFrame(4).GetFileLineNumber() : -1;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
    }

    private static string Formatted(this string? value)
        => value is null ? "<Null>" : $"\"{value}\"";

    private static string FormatAsValueOrClass(object value)
    {
        // is it a class, we render the object with an identifier?
        if ((value.GetType().IsClass && value is not string) || value is Type)
            return AssertFailures.AsObjectId(value);

        // fallback to default formatting
        var saveCulture = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            return value.ToString() ?? "<Null>";
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = saveCulture;
        }
    }

    private static string Formatted<TValue>(this TValue?[] args, int indentation = 0)
        => args.Length == 0
            ? "<Empty>"
            : "[" + string.Join(", ", args.ToArray().Select(v => Formatted(v))).Indentation(indentation) + "]";

    private static string Formatted(this IEnumerable args, int indentation = 0)
        => Formatted(args.Cast<object?>().ToArray(), indentation);

    [GeneratedRegex("\\[/?(b|color).*?\\]")]
    private static partial Regex RegexTextNormalize();

    [GeneratedRegex("(\\p{Ll})(\\p{Lu})")]
    private static partial Regex RegexToSnakeCase();
}
