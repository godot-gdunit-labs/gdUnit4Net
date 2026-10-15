// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Analyzers;

using Microsoft.CodeAnalysis;

/// <summary>
///     Provides diagnostic descriptors for GdUnit4 analyzer rules.
///     These rules help enforce the correct usage of GdUnit4 test attributes
///     and identify potential issues during compilation time.
/// </summary>
internal static class DiagnosticRules
{
    private const string HELP_LINK = "https://github.com/MikeSchulze/gdUnit4Net/tree/master/Analyzers/documentation";

    /// <summary>
    ///     Contains the identifiers for GdUnit4 analyzer rules.
    /// </summary>
    internal static class RuleIds
    {
        // Rule IDs (GdUnit01xx)
        // public const string TestCaseName = "GdUnit0101";

        /// <summary>
        ///     Rule ID for detecting when a method with a DataPoint attribute has multiple TestCase attributes.
        /// </summary>
        public const string DATA_POINT_WITH_MULTIPLE_TEST_CASE = "GdUnit0201";

        /// <summary>
        ///     Rule ID for detecting multiple TestCase attributes with the same TestName on the same method.
        /// </summary>
        public const string TESTCASE_NAMES_MUST_BE_UNIQUE_ID = "GdUnit0202";

        /// <summary>
        ///     Rule ID for detecting when a test class using Godot functionality needs the RequireGodotRuntime attribute.
        /// </summary>
        public const string REQUIRES_GODOT_RUNTIME_ON_CLASS_ID = "GdUnit0500";

        /// <summary>
        ///     Rule ID for detecting when a test method using Godot functionality needs the RequireGodotRuntime attribute.
        /// </summary>
        public const string REQUIRES_GODOT_RUNTIME_ON_METHOD_ID = "GdUnit0501";
    }

    /// <summary>
    ///     Provides diagnostic descriptors for DataPoint-related rules.
    ///     These rules ensure proper usage of DataPoint attributes in test methods.
    /// </summary>
    internal static class DataPoint
    {
        /// <summary>
        ///     Diagnostic rule for detecting when a method with a DataPoint attribute has multiple TestCase attributes.
        ///     DataPoint methods should only have a single TestCase attribute to avoid undefined behavior.
        /// </summary>
        public static readonly DiagnosticDescriptor MultipleTestCaseAttributes = new(
            RuleIds.DATA_POINT_WITH_MULTIPLE_TEST_CASE,
            "Multiple TestCase attributes not allowed with DataPoint",
            "Method '{0}' cannot have multiple TestCase attributes when DataPoint attribute is present",
            Categories.ATTRIBUTE_USAGE,
            DiagnosticSeverity.Error,
            true,
            "Methods decorated with DataPoint attribute can only have one TestCase attribute. Multiple TestCase attributes on a method that uses DataPoint will result in undefined behavior.",
            $"{HELP_LINK}/{RuleIds.DATA_POINT_WITH_MULTIPLE_TEST_CASE}.md",
            WellKnownDiagnosticTags.Compiler);

        // Future TestCase rules can be added here
    }

    /// <summary>
    ///     Provides diagnostic descriptors for Godot runtime requirement rules.
    ///     These rules ensure that tests using Godot functionality are properly annotated.
    /// </summary>
    internal static class GodotEngine
    {
        /// <summary>
        ///     Diagnostic rule for detecting when a test method using Godot functionality
        ///     is missing the RequireGodotRuntime attribute.
        /// </summary>
        public static readonly DiagnosticDescriptor RequiresGodotRuntimeOnMethod = new(
            RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID,
            "Godot Runtime Required for Test Method",
            "Test method '{0}' uses Godot functionality but is not annotated with `[RequireGodotRuntime]`",
            Categories.ATTRIBUTE_USAGE,
            DiagnosticSeverity.Error,
            true,
            """
            Test methods that use Godot functionality (such as Node, Scene, or other Godot types)
            must be annotated with `[RequireGodotRuntime]`. This ensures proper test execution in
            the Godot engine environment.

            Add `[RequireGodotRuntime]` to either test method or class level.
            """,
            $"{HELP_LINK}/{RuleIds.REQUIRES_GODOT_RUNTIME_ON_METHOD_ID}.md",
            WellKnownDiagnosticTags.Compiler);

        /// <summary>
        ///     Diagnostic rule for detecting when a test class with hooks using Godot functionality
        ///     is missing the RequireGodotRuntime attribute.
        /// </summary>
        public static readonly DiagnosticDescriptor RequiresGodotRuntimeOnClass = new(
            RuleIds.REQUIRES_GODOT_RUNTIME_ON_CLASS_ID,
            "Godot Runtime Required for Test Class",
            "Test class '{0}' uses Godot native types or calls in test hooks but is not annotated with `[RequireGodotRuntime]`",
            Categories.ATTRIBUTE_USAGE,
            DiagnosticSeverity.Error,
            true,
            """
            Test classes with hooks (`[Before]`, `[After]`, `[BeforeTest]`, `[AfterTest]`) that use Godot functionality
            must be annotated with `[RequireGodotRuntime]`. This ensures proper test execution in the
            Godot engine environment.

            Add `[RequireGodotRuntime]` to the test class level.
            """,
            $"{HELP_LINK}/{RuleIds.REQUIRES_GODOT_RUNTIME_ON_CLASS_ID}.md",
            WellKnownDiagnosticTags.Compiler);
    }

    internal static class TestCase
    {
        public static readonly DiagnosticDescriptor TestCaseNamesMustBeUnique = new(
            RuleIds.TESTCASE_NAMES_MUST_BE_UNIQUE_ID,
            "Test case names must be unique",
            "Test method '{0}' has multiple TestCase attributes with the same TestName",
            Categories.ATTRIBUTE_USAGE,
            DiagnosticSeverity.Error,
            true,
            """
            Multiple TestCase attributes on a test method cannot specify the same TestName. Duplicate test names will skip the tests and cause inconclusive results.
            """,
            $"{HELP_LINK}/{RuleIds.TESTCASE_NAMES_MUST_BE_UNIQUE_ID}.md",
            WellKnownDiagnosticTags.Compiler);
    }

    /// <summary>
    ///     Defines diagnostic categories used for grouping rules.
    ///     These categories help organize rules by their purpose or area of concern.
    /// </summary>
    private static class Categories
    {
        /// <summary>
        ///     Category for rules related to proper usage of attributes.
        /// </summary>
        public const string ATTRIBUTE_USAGE = "Attribute Usage";
    }
}
