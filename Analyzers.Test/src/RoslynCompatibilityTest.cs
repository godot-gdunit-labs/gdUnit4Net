namespace GdUnit4.Analyzers.Test;

using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public sealed class RoslynCompatibilityTest
{
    // Roslyn 4.8 ships with the .NET SDK 8.0.100 and Visual Studio 17.8, an older compiler does not load the analyzers
    private static readonly Version MaxRoslynVersion = new(4, 8, 0, 0);

    [TestMethod]
    [DataRow("Microsoft.CodeAnalysis")]
    [DataRow("Microsoft.CodeAnalysis.CSharp")]
    public void AnalyzersReferenceNoNewerRoslynVersion(string assemblyName)
    {
        var reference = typeof(UniqueTestCaseNameAnalyzer).Assembly
            .GetReferencedAssemblies()
            .SingleOrDefault(name => name.Name == assemblyName);

        Assert.IsNotNull(reference, $"The analyzers do not reference '{assemblyName}'.");
        Assert.IsTrue(
            reference.Version <= MaxRoslynVersion,
            $"The analyzers reference '{assemblyName}' {reference.Version}, which needs a newer compiler than Roslyn {MaxRoslynVersion}.");
    }
}
