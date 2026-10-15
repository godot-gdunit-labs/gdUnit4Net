// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Discovery;

using System.Text.Json;

using Execution;

/// <summary>
///     Describes a discovered test case with its location and execution requirements.
/// </summary>
public sealed class TestCaseDescriptor : IEquatable<TestCaseDescriptor>
{
    private static readonly JsonSerializerOptions ToStringOptions = new() { WriteIndented = true };

    /// <summary>
    ///     Gets or sets a simple display name of the test case.
    /// </summary>
    public string SimpleName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets a fully qualified name including class path and test parameters.
    /// </summary>
    public string FullyQualifiedName { get; set; } = string.Empty;

    /// <summary>
    ///     Gets path to the assembly containing this test case.
    /// </summary>
    public required string AssemblyPath { get; init; }

    /// <summary>
    ///     Gets a fully qualified name of the class containing this test.
    /// </summary>
    public required string ManagedType { get; init; }

    /// <summary>
    ///     Gets the name of the test method.
    /// </summary>
    public required string ManagedMethod { get; init; }

    /// <summary>
    ///     Gets a unique identifier for this test case.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    ///     Gets the line number in a source file where the test is defined.
    /// </summary>
    public required int LineNumber { get; init; }

    /// <summary>
    ///     Gets path to source code file containing this test.
    /// </summary>
    public required string? CodeFilePath { get; init; }

    /// <summary>
    ///     Gets index if multiple test cases exist for the same method.
    /// </summary>
    public required int AttributeIndex { get; init; }

    /// <summary>
    ///     Gets a value indicating whether this test requires the Godot engine to be running.
    /// </summary>
    public required bool RequireRunningGodotEngine { get; init; }

    /// <summary>
    ///     Gets the list of categories associated with this test case.
    ///     Categories are set by the TestCategory attributes.
    /// </summary>
    public IReadOnlyCollection<string> Categories { get; init; } = [];

    /// <summary>
    ///     Gets the collection of traits associated with this test case.
    ///     Each trait is a name-value pair that can be used for filtering and reporting.
    /// </summary>
    public IReadOnlyDictionary<string, List<string>> Traits { get; init; } = new Dictionary<string, List<string>>();

    /// <summary>
    ///     Determines whether the specified <see cref="TestCaseDescriptor" /> is equal to the current instance.
    /// </summary>
    /// <param name="other">The test case descriptor to compare with the current instance.</param>
    /// <returns>
    ///     <see langword="true" /> if the specified descriptor is equal to the current instance;
    ///     otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(TestCaseDescriptor? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return SimpleName == other.SimpleName
               && FullyQualifiedName == other.FullyQualifiedName
               && AssemblyPath == other.AssemblyPath
               && ManagedType == other.ManagedType
               && ManagedMethod == other.ManagedMethod
               && Id == other.Id
               && LineNumber == other.LineNumber
               && AttributeIndex == other.AttributeIndex
               && RequireRunningGodotEngine == other.RequireRunningGodotEngine
               && Categories.SequenceEqual(other.Categories)
               && Traits.Count == other.Traits.Count
               && Traits.Keys.All(key =>
                   other.Traits.ContainsKey(key) && Traits[key].SequenceEqual(other.Traits[key]));
    }

    /// <summary>
    ///     Determines whether two <see cref="TestCaseDescriptor" /> instances are equal.
    /// </summary>
    /// <param name="left">The first test case descriptor to compare.</param>
    /// <param name="right">The second test case descriptor to compare.</param>
    /// <returns>
    ///     <see langword="true" /> if the descriptors are equal; otherwise, <see langword="false" />.
    /// </returns>
#pragma warning disable SA1201
    public static bool operator ==(TestCaseDescriptor? left, TestCaseDescriptor? right)
    {
        if (left is null)
            return right is null;
        return left.Equals(right);
    }
#pragma warning restore SA1201

    /// <summary>
    ///     Determines whether two <see cref="TestCaseDescriptor" /> instances are not equal.
    /// </summary>
    /// <param name="left">The first test case descriptor to compare.</param>
    /// <param name="right">The second test case descriptor to compare.</param>
    /// <returns>
    ///     <see langword="true" /> if the descriptors are not equal; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator !=(TestCaseDescriptor? left, TestCaseDescriptor? right)
        => !(left == right);

    /// <summary>
    ///     Determines whether the specified object is equal to the current <see cref="TestCaseDescriptor" />.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns>
    ///     <see langword="true" /> if the specified object is a <see cref="TestCaseDescriptor" />
    ///     and is equal to the current instance; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        return obj is TestCaseDescriptor descriptor && Equals(descriptor);
    }

    /// <summary>
    ///     Returns a hash code for the current <see cref="TestCaseDescriptor" />.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        hashCode.Add(AssemblyPath);
        hashCode.Add(ManagedType);
        hashCode.Add(ManagedMethod);
        hashCode.Add(Id);
        hashCode.Add(LineNumber);
        hashCode.Add(AttributeIndex);
        hashCode.Add(RequireRunningGodotEngine);
        hashCode.Add(Categories.Count);
        hashCode.Add(Traits.Count);
        return hashCode.ToHashCode();
    }

    /// <summary>
    ///     Returns a JSON string representation of the test case descriptor.
    /// </summary>
    /// <returns>A formatted JSON string containing all properties of the test case descriptor.</returns>
    /// <remarks>
    ///     This method is primarily used for debugging and logging purposes.
    ///     The JSON output includes all properties with indented formatting for readability.
    /// </remarks>
    public override string ToString() => JsonSerializer.Serialize(this, ToStringOptions);

    internal TestCaseDescriptor Build(TestCaseAttribute testCaseAttribute, bool hasMultipleAttributes)
    {
        SimpleName = TestCase.BuildDisplayName(ManagedMethod, testCaseAttribute, hasMultipleAttributes ? AttributeIndex : -1);
        FullyQualifiedName = hasMultipleAttributes
            ? $"{ManagedType}.{ManagedMethod}.{SimpleName}"
            : $"{ManagedType}.{SimpleName}";
        return this;
    }
}
