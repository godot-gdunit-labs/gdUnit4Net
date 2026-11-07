// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

/// <summary>
///     Attribute used to define test cases, including both simple and parameterized tests.
///     This attribute can be used to mark a method as a test or to execute a test method multiple times with different input arguments.
///     Multiple instances can be applied to a single test method to create various test scenarios.
/// </summary>
/// <remarks>
///     Test cases can be used without arguments as a simple test marker, or customized with specific arguments, random seeds, and iteration counts for parameterized testing.
/// </remarks>
/// <example>
///     <code>
/// // Simple test case
/// [TestCase]
/// public void SimpleTest()
/// {
///     // Test implementation
/// }
///
/// // Parameterized test cases
/// [TestCase(10, "example")]
/// [TestCase(20, "another example", Seed = 42.0, Iterations = 5)]
/// public void TestWithParameters(int value, string text)
/// {
///     // Test implementation using the parameters
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class TestCaseAttribute : TestStageAttribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestCaseAttribute" /> class with the specified arguments.
    /// </summary>
    /// <param name="arguments">The arguments to pass to the test method during execution.</param>
    public TestCaseAttribute(params object?[]? arguments)
        : base(string.Empty, -1)
        => Arguments = arguments ?? [null];

    /// <summary>
    ///     Gets or sets the starting point of random values by given seed.
    /// </summary>
    public double Seed { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the number of test iterations for a parameterized test.
    /// </summary>
    public int Iterations { get; set; } = 1;

    /// <summary>
    ///     Gets or sets an optional test case name to override the original test case name.
    /// </summary>
    public string? TestName { get; set; }

    /// <summary>
    ///     Gets the test case argument when is specified.
    /// </summary>
    public object?[] Arguments { get; private set; }
}
