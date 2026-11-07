// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

using System;

/// <summary>
///     Provides data-driven test capabilities by specifying a data source for a test method.
///     This attribute allows tests to be executed multiple times with different input data.
/// </summary>
/// <remarks>
///     The DataPointAttribute can be used to source test data from:
///     <list type="bullet">
///         <item>
///             <term>Static Properties</term>
///             <description>
///                 Properties that return IEnumerable{object[]} or IAsyncEnumerable{object[]} for multiple parameters,
///                 or IEnumerable{T}/IAsyncEnumerable{T} for single parameters.
///             </description>
///         </item>
///         <item>
///             <term>Static Methods</term>
///             <description>
///                 Methods that return test data, including parameterized methods for dynamic data generation
///                 and async methods for asynchronous data delivery.
///             </description>
///         </item>
///     </list>
///     Example usages:
///     <code>
/// public class TestClass
/// {
///     // Property data source
///     public static IEnumerable{object[]} TestData => new[]
///     {
///         new object[] { 1, 2, 3 },
///         new object[] { 4, 5, 9 }
///     };
///
///     // Method data source
///     public static IEnumerable{object[]} GetTestData(int factor) => new[]
///     {
///         new object[] { 1*factor, 2 },
///         new object[] { 2*factor, 4 }
///     };
///
///     // Async data source
///     public static async IAsyncEnumerable{object?[]} AsyncData()
///     {
///         await Task.Delay(10);
///         yield return new object?[] { 1, 2, 3 };
///         await Task.Delay(10);
///         yield return new object?[] { 4, 5, 9 };
///     }
///
///     // Test method using property data source
///     [TestCase]
///     [DataPoint(nameof(TestData))]
///     public void Test(int a, int b, int expected)
///     {
///         AssertThat(a + b).IsEqual(expected);
///     }
///
///     // Test method using method data source with parameters
///     [TestCase]
///     [DataPoint(nameof(GetTestData), 2)]
///     public void TestWithParameter(int value, int expected)
///     {
///         AssertThat(value).IsEqual(expected);
///     }
///
///     // Test method using data source from different class
///     [TestCase]
///     [DataPoint(nameof(TestData), typeof(ExternalDataClass))]
///     public void TestWithExternalData(int a, int b, int expected)
///     {
///         AssertThat(a + b).IsEqual(expected);
///     }
/// }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class DataPointAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DataPointAttribute" /> class with a data source.
    /// </summary>
    /// <param name="dataPointSource">The name of the property or method that provides the test data.</param>
    public DataPointAttribute(string dataPointSource)
    {
        DataPointSource = dataPointSource;
        DataPointParameters = null;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DataPointAttribute" /> class with a data source from a specified type.
    /// </summary>
    /// <param name="dataPointSource">The name of the property or method that provides the test data.</param>
    /// <param name="dataPointDeclaringType">The type that contains the data source.</param>
    public DataPointAttribute(string dataPointSource, Type dataPointDeclaringType)
    {
        DataPointDeclaringType = dataPointDeclaringType;
        DataPointSource = dataPointSource;
        DataPointParameters = null;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DataPointAttribute" /> class with a data source and parameters.
    /// </summary>
    /// <param name="dataPointSource">The name of the method that provides the test data.</param>
    /// <param name="dataPointParameters">Arguments to pass to the data source method.</param>
    public DataPointAttribute(string dataPointSource, params object?[] dataPointParameters)
    {
        DataPointSource = dataPointSource;
        DataPointParameters = dataPointParameters;
    }

    /// <summary>
    ///     Gets the type that contains the data source method or property.
    ///     If null, the test class is used.
    /// </summary>
    public Type? DataPointDeclaringType { get; }

    /// <summary>
    ///     Gets the name of the method or property that provides the test data.
    /// </summary>
    public string DataPointSource { get; }

    /// <summary>
    ///     Gets the arguments to pass to the data source method.
    ///     Will be null for properties or methods without parameters.
    /// </summary>
    public object?[]? DataPointParameters { get; }
}
