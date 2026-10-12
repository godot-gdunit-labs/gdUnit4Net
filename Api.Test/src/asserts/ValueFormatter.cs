namespace GdUnit4.Tests.Asserts;

using GdUnit4.Asserts;

public static class ValueFormatter
{
    internal static string AsString(object? value)
    {
        if (value == null)
            return "NULL";
        if (value is string s)
            return $"\"{s}\"";
        if (value.GetType().IsPrimitive)
            return value.ToString() ?? "NULL";

        return AssertFailures.AsObjectId(value);
    }
}
