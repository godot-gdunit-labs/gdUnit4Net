// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Analyzers;

using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using static DiagnosticRules;

/// <summary>
///     Analyzer that checks for improper usage of DataPoint attributes.
///     Specifically, it ensures methods with DataPointAttribute don't have multiple TestCaseAttributes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class DataPointAttributeAnalyzer : DiagnosticAnalyzer
{
    /// <summary>
    ///     Gets the set of diagnostic descriptors supported by this analyzer.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DataPoint.MultipleTestCaseAttributes];

    /// <summary>
    ///     Initializes the analyzer with the analysis context.
    /// </summary>
    /// <param name="context">The analysis context to initialize.</param>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(ctx =>
        {
            var dataPointAttr = ctx.Compilation.GetTypeByMetadataName("GdUnit4.DataPointAttribute");
            var testCaseAttr = ctx.Compilation.GetTypeByMetadataName("GdUnit4.TestCaseAttribute");

            // Do nothing if this assembly doesn't have access to our attributes.
            if (dataPointAttr is null || testCaseAttr is null)
                return;

            ctx.RegisterSymbolAction(symbolContext => AnalyzeMethodSymbol(symbolContext, dataPointAttr, testCaseAttr), SymbolKind.Method);
        });

    }

    /// <summary>
    ///     Analyzes a method symbol to verify proper usage of DataPoint and TestCase attributes.
    /// </summary>
    /// <param name="context">The symbol analysis context containing the method to analyze.</param>
    /// <param name="dataPointAttr">The INamedTypeSymbol representing DataPointAttribute.</param>
    /// <param name="testCaseAttr">The INamedTypeSymbol representing TestCaseAttribute.</param>
    private static void AnalyzeMethodSymbol(SymbolAnalysisContext context, INamedTypeSymbol dataPointAttr, INamedTypeSymbol testCaseAttr)
    {
        var methodSymbol = (IMethodSymbol)context.Symbol;

        // Check for DataPoint attribute
        var hasDataPoint = methodSymbol.GetAttributes()
            .Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, dataPointAttr));

        if (!hasDataPoint)
            return;

        // Get all TestCase attributes with their ApplicationSyntaxReference
        var testCaseAttributes = methodSymbol.GetAttributes()
            .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, testCaseAttr))
            .Skip(1); // Skip the first TestCase attribute, one test case attribute is required

        // Report on all TestCase attributes after the first one
        foreach (var testCaseAttribute in testCaseAttributes)
        {
            if (testCaseAttribute.ApplicationSyntaxReference?.GetSyntax() is { } syntaxNode)
            {
                var diagnostic = Diagnostic.Create(
                    DataPoint.MultipleTestCaseAttributes,
                    syntaxNode.GetLocation(),
                    methodSymbol.Name);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
