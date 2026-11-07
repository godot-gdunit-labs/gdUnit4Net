// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

using System;

/// <summary>
///     Specifies a category for a test method or class.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class TestCategoryAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TestCategoryAttribute" /> class.
    /// </summary>
    /// <param name="category">The name of the category.</param>
    public TestCategoryAttribute(string category) => Category = category;

    /// <summary>
    ///     Gets the name of the category.
    /// </summary>
    public string Category { get; }
}
