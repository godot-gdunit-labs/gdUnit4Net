// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

/// <summary>
///     Attribute used to temporarily ignore tests until a certain condition is met or until further development.
///     Tests marked with this attribute will be skipped during test execution.
/// </summary>
/// <remarks>
///     This attribute is useful for tests that are temporarily failing due to known issues
///     or tests that depend on features which haven't been implemented yet.
/// </remarks>
/// <example>
///     <code>
/// [Test]
/// [IgnoreUntil(Until = "2025-12-31 15:00:00")]  // Uses local time
/// public void TestFeatureNotImplementedYet()
/// {
///     // Test code that will be skipped until Dec 31, 2025 at 15:00 local time
/// }
///
/// [Test]
/// [IgnoreUntil(UntilUtc = "2025-12-31 15:00:00")]  // Uses UTC time
/// public void TestWithUtcTime()
/// {
///     // Test code that will be skipped until Dec 31, 2025 at 15:00 UTC
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class IgnoreUntilAttribute : TestStageAttribute
{
    private DateTime untilDateUtc = DateTime.MaxValue;

    /// <summary>
    ///     Initializes a new instance of the <see cref="IgnoreUntilAttribute" /> class.
    /// </summary>
    /// <param name="line">The line number where the attribute is applied (automatically provided).</param>
    /// <param name="name">The name of the method where the attribute is applied (automatically provided).</param>
#pragma warning disable CA1019
    public IgnoreUntilAttribute([CallerLineNumber] int line = 0, [CallerMemberName] string name = "")
#pragma warning restore CA1019
        : base(name, line)
    {
    }

    /// <summary>
    ///     Gets or Sets the date/time until which to ignore the test, interpreted as local time.
    ///     Format: "yyyy-MM-dd" or "yyyy-MM-dd HH:mm:ss".
    /// </summary>
    public string Until
    {
        get => string.Empty;
        set => untilDateUtc = DateTime
            .Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal)
            .ToUniversalTime();
    }

    /// <summary>
    ///     Gets or Sets the date/time until which to ignore the test, interpreted as UTC time.
    ///     Format: "yyyy-MM-dd" or "yyyy-MM-dd HH:mm:ss".
    /// </summary>
    public string UntilUtc
    {
        get => string.Empty;
        set => untilDateUtc = DateTime.Parse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    /// <summary>
    ///     Checks if a method should be skipped based on the IgnoreUntilAttribute.
    /// </summary>
    /// <param name="methodInfo">The method to check.</param>
    /// <returns>True if the method should be skipped, false otherwise.</returns>
    internal static bool IsSkipped(MethodInfo methodInfo)
    {
        var ignoreAttribute = methodInfo.GetCustomAttribute<IgnoreUntilAttribute>();
        return ignoreAttribute?.ShouldSkip() ?? false;
    }

    /// <summary>
    ///     Determines if the test should be skipped based on the current date.
    /// </summary>
    /// <returns>True if the current date is before the Until date, false otherwise.</returns>
    private bool ShouldSkip() => DateTime.UtcNow < untilDateUtc;
}
