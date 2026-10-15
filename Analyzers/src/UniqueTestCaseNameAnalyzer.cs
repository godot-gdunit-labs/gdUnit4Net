// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Analyzers;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using static DiagnosticRules;

/// <summary>
///
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UniqueTestCaseNameAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [TestCase.TestCaseNamesMustBeUnique];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var testCaseAttr = compilationContext.Compilation.GetTypeByMetadataName("GdUnit4.TestCaseAttribute");
            if (testCaseAttr is null)
                return;

            compilationContext.RegisterSyntaxNodeAction(ctx => AnalyzeMethod(ctx, testCaseAttr), SyntaxKind.MethodDeclaration);
        });
    }


    private void AnalyzeMethod(SyntaxNodeAnalysisContext context, INamedTypeSymbol testCaseAttr)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.Node;

        var containingSymbol = context.ContainingSymbol;
        if (containingSymbol is null)
            return;

        // Get all TestCase attributes on the method.
        var testCaseAttributes = containingSymbol.GetAttributes()
            .Where(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, testCaseAttr));

        HashSet<string> usedNames = [];
        foreach (var attr in testCaseAttributes)
        {
            // Get the explicitly-set TestName value.
            if (attr.NamedArguments.Single(kvp => kvp.Key == "TestName").Value.Value is not string name)
                continue;

            // Check if we've already encountered this name.
            if (!usedNames.Add(name))
            {
                if (attr.ApplicationSyntaxReference?.GetSyntax() is { } syntaxNode)
                {
                    // Report diagnostic
                    var diagnostic = Diagnostic.Create(
                        TestCase.TestCaseNamesMustBeUnique,
                        syntaxNode.GetLocation(),
                        containingSymbol.Name
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

}
