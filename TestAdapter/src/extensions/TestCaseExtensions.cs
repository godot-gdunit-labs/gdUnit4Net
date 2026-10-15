// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter.Extensions;

using Core.Discovery;

using Microsoft.TestPlatform.AdapterUtilities;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
///     Extension Methods for TestCase Class.
/// </summary>
internal static class TestCaseExtensions
{
    internal static readonly TestProperty NamespaceProperty = TestProperty.Register(
        "TestCase.Namespace",
        "Namespace",
        string.Empty,
        "The namespace containing the test",
        typeof(string),
        o => o is string,
        TestPropertyAttributes.Hidden,
        typeof(TestCase));

    internal static readonly TestProperty ClassProperty = TestProperty.Register(
        "TestCase.Class",
        "Class",
        string.Empty,
        "holds the test suite class name",
        typeof(string),
        o => !string.IsNullOrWhiteSpace(o as string),
        TestPropertyAttributes.Hidden,
        typeof(TestCase));

    internal static readonly TestProperty ManagedTypeProperty = TestProperty.Register(
        ManagedNameConstants.ManagedTypePropertyId,
        ManagedNameConstants.ManagedTypeLabel,
        string.Empty,
        "holds the test suite class name",
        typeof(string),
        o => !string.IsNullOrWhiteSpace(o as string),
        TestPropertyAttributes.Hidden,
        typeof(TestCase));

    internal static readonly TestProperty ManagedMethodProperty = TestProperty.Register(
        ManagedNameConstants.ManagedMethodPropertyId,
        ManagedNameConstants.ManagedMethodLabel,
        string.Empty,
        "holds the test method name",
        typeof(string),
        o => !string.IsNullOrWhiteSpace(o as string),
        TestPropertyAttributes.Hidden,
        typeof(TestCase));

    internal static readonly TestProperty ManagedMethodAttributeIndexProperty = TestProperty.Register(
        "TestCase.ManagedMethodAttributeIndex",
        "ManagedAttributeIndex",
        string.Empty,
        "Holds the test method attribute index",
        typeof(int),
        value => value is int and >= 0,
        TestPropertyAttributes.Hidden,
        typeof(TestCase));

    internal static readonly TestProperty RequireRunningGodotEngineProperty = TestProperty.Register(
        "TestCase.RequireRunningGodotEngine",
        "RequireRunningGodotEngine",
        string.Empty,
        "Indicates that the test method must execute inside Godot engine context.",
        typeof(bool),
        value => value is bool,
        TestPropertyAttributes.Hidden,
        typeof(TestCase));

    // Added property for category filtering
    internal static readonly TestProperty TestCategoryProperty = TestProperty.Register(
        "TestCase.TestCategory",
        "TestCategory",
        typeof(string[]), // "The category assigned to the test",
        TestPropertyAttributes.Immutable,
        typeof(TestCase));

    internal static readonly TestProperty HierarchyProperty = TestProperty.Register(
        HierarchyConstants.HierarchyPropertyId,
        HierarchyConstants.HierarchyLabel,
        string.Empty,
        string.Empty,
        typeof(string[]),
        null,
        TestPropertyAttributes.Immutable,
        typeof(TestCase));

    // Filter properties for use in test filtering
    internal static readonly Dictionary<string, TestProperty> SupportedProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        [TestCaseProperties.DisplayName.Label] = TestCaseProperties.DisplayName,
        [TestCaseProperties.FullyQualifiedName.Label] = TestCaseProperties.FullyQualifiedName,
        [NamespaceProperty.Label] = NamespaceProperty,
        [ClassProperty.Label] = ClassProperty,
        [ManagedMethodProperty.Label] = ManagedMethodProperty,
        [ManagedMethodAttributeIndexProperty.Label] = ManagedMethodAttributeIndexProperty,
        [RequireRunningGodotEngineProperty.Label] = RequireRunningGodotEngineProperty,
        [HierarchyProperty.Label] = HierarchyProperty,
        [TestCategoryProperty.Label] = TestCategoryProperty
    };

    public static void AddTrait(this TestCase testCase, string name, string value) =>
        testCase.Traits.Add(new Trait(name, value));

    public static void SetPropertyValues(this TestCase testCase, TestCaseDescriptor descriptor)
    {
        var (namespaceName, className) = SplitByNamespace(descriptor.ManagedType);
        var hierarchyValues = new string[HierarchyConstants.Levels.TotalLevelCount];
        hierarchyValues[HierarchyConstants.Levels.ContainerIndex] = Path.GetFileNameWithoutExtension(descriptor.AssemblyPath);
        hierarchyValues[HierarchyConstants.Levels.NamespaceIndex] = namespaceName;
        hierarchyValues[HierarchyConstants.Levels.ClassIndex] = className;
        hierarchyValues[HierarchyConstants.Levels.TestGroupIndex] = descriptor.ManagedMethod;
        testCase.SetPropertyValue(HierarchyProperty, hierarchyValues);
        testCase.SetPropertyValue(NamespaceProperty, namespaceName);
        testCase.SetPropertyValue(ClassProperty, descriptor.ManagedType);
        testCase.SetPropertyValue(ManagedTypeProperty, descriptor.ManagedType);
        testCase.SetPropertyValue(ManagedMethodProperty, descriptor.ManagedMethod);
        testCase.SetPropertyValue(TestCaseProperties.FullyQualifiedName, descriptor.FullyQualifiedName);
        testCase.SetPropertyValue(ManagedMethodAttributeIndexProperty, descriptor.AttributeIndex);
        testCase.SetPropertyValue(RequireRunningGodotEngineProperty, descriptor.RequireRunningGodotEngine);
        testCase.SetPropertyValue(TestCategoryProperty, descriptor.Categories.ToArray());

        // Add traits
        descriptor.Traits
            .SelectMany(group => group.Value, (group, value) => new Trait(group.Key, value))
            .ToList()
            .ForEach(testCase.Traits.Add);
    }

    /// <summary>
    ///     Gets the property provider function for use with GetTestCaseFilter.
    /// </summary>
    /// <returns>A function that returns a TestProperty for a given property name.</returns>
    public static Func<string, TestProperty?> GetPropertyProvider() =>
        propertyName => SupportedProperties.GetValueOrDefault(propertyName);

    /// <summary>
    ///     Gets the value of a property from a test case.
    /// </summary>
    /// <param name="testCase">The test case.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>The property value or null if not found.</returns>
    internal static object? GetPropertyValue(this TestCase testCase, string propertyName) =>
        SupportedProperties.TryGetValue(propertyName, out var testProperty)
            ? testCase.GetPropertyValue(testProperty)
            : testCase.GetPropertyTraits(propertyName);

    private static string[]? GetPropertyTraits(this TestCase testCase, string propertyName)
    {
        if (!testCase.Traits.Any() || !propertyName.StartsWith("Trait.", StringComparison.OrdinalIgnoreCase))
            return null;
        var traitName = propertyName["Trait.".Length..];

        return
        [
            .. testCase.Traits
                .Where(t => string.Equals(t.Name, traitName, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.Value)
        ];
    }

    private static (string NamespaceName, string ClassName) SplitByNamespace(string managedType)
    {
        var parts = managedType.Split('.');
        var namespaceName = parts.Length == 1 ? string.Empty : string.Join(".", parts.Take(parts.Length - 1));
        var className = parts.Last();
        return (namespaceName, className);
    }
}
