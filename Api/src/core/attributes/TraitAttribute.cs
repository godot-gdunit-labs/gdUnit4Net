// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

// ReSharper disable once CheckNamespace
// Need to be placed in the root namespace to be accessible by the test runner.
namespace GdUnit4;

using System;

/// <summary>
///     Specifies a trait for a test method or class, allowing tests to be categorized and filtered.
/// </summary>
/// <remarks>
///     Traits can be used to categorize tests by various criteria such as category, priority, or area.
///     Multiple traits can be applied to the same test method or class.
///     Traits are useful for selectively running tests based on their characteristics.
/// </remarks>
/// <example>
///     <code>
/// [TestSuite]
/// [Trait("Category", "Integration")]
/// public class DatabaseTests
/// {
///     [Test]
///     [Trait("Priority", "High")]
///     [Trait("Feature", "Authentication")]
///     public void UserLogin_ValidCredentials_Succeeds()
///     {
///         // Test implementation
///     }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class TraitAttribute : Attribute
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TraitAttribute" /> class.
    /// </summary>
    /// <param name="name">The name of the trait.</param>
    /// <param name="value">The value of the trait.</param>
    public TraitAttribute(string name, string value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    ///     Gets the name of the trait.
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     Gets the value of the trait.
    /// </summary>
    public string Value { get; }
}
