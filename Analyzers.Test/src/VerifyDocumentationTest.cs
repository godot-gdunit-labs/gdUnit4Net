namespace GdUnit4.Analyzers.Test;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public partial class VerifyDocumentationTest
{
    private static readonly string ProjectRoot = Directory.GetCurrentDirectory()
        .Split(Path.DirectorySeparatorChar)
        .TakeWhile(dir => dir != "Analyzers.Test")
        .Concat(["Analyzers"])
        .Aggregate(Path.Combine);

    [TestMethod]
    public void VerifyIndexDocumentation()
    {
        var indexPath = Path.Combine(ProjectRoot, "documentation", "index.md");
        if (!OperatingSystem.IsWindows())
            indexPath = Path.Combine("/", indexPath);

        Assert.IsTrue(File.Exists(indexPath), $"Index file not found: {indexPath}");

        var indexContent = File.ReadAllText(indexPath);
        var lines = indexContent.Split('\n').Select(l => l.Trim()).ToList();

        // Get all diagnostic descriptors
        var diagnostics = GetAllDiagnosticDescriptors().ToList();

        // Skip to table content (after header row and separator)
        var tableStart = lines.FindIndex(l => l.StartsWith("| Id ", StringComparison.InvariantCulture)) + 2;
        Assert.IsTrue(tableStart > 1, "Index file does not contain the expected table format");

        var records = ParseMarkdownTable(lines.GetRange(tableStart, lines.Count - tableStart));

        foreach ((var descriptor, var _) in diagnostics)
        {
            var ruleId = $"[{descriptor.Id}]({descriptor.Id}.md)";
            var recordIndex = records.Find(record => record.Id.Equals(ruleId, StringComparison.Ordinal));
            Assert.IsNotNull(recordIndex, $"Missing table entry for rule {descriptor.Id}.");
            var expected = new DiagnosticRuleTableRecord
            {
                Id = ruleId,
                Title = descriptor.Title.ToString(),
                Severity = descriptor.DefaultSeverity.ToString()
            };
            Assert.AreEqual(expected, recordIndex, $"Diagnostic rule record was not found in '{indexPath}'.");

            // verify the documentation rule file exists
            CheckDocumentationFile(descriptor);
        }
    }

    private static void CheckDocumentationFile(DiagnosticDescriptor descriptor)
    {
        var docLink = descriptor.HelpLinkUri
            .Replace("https://github.com/MikeSchulze/gdUnit4Net/tree/master/Analyzers/", string.Empty, StringComparison.Ordinal)
            .Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal);
        var docPath = Path.Combine(Path.DirectorySeparatorChar.ToString(), ProjectRoot, docLink);
        Assert.IsTrue(File.Exists(docPath), $"Documentation file not found: {docPath}\nExpected documentation for rule {descriptor.Id}");

        // Compare with normalized line endings: the documentation follows the checkout line endings, the C# sources are always LF
        var docContent = File.ReadAllText(docPath).ReplaceLineEndings("\n");

        // Check if the first line of the document contains the required header
        var lines = docContent.Split('\r', '\n');
        var firstLine = lines.FirstOrDefault();
        var expectedHeader = $"# {descriptor.Id}";

        Assert.IsTrue(firstLine == expectedHeader, $"required header '{expectedHeader}' on the first line in {docPath}.");
        Assert.IsTrue(lines.Length > 2 && string.IsNullOrEmpty(lines[1]), $"Expected an empty line after the header in {docPath}.");
        StringAssert.Contains(docContent, "# Problem Description", $"Documentation should contain ` Problem Description` section in {docPath}", StringComparison.Ordinal);
        StringAssert.Contains(docContent, $"## {descriptor.Title}", $"Documentation should contain the diagnostic title in {docPath}", StringComparison.Ordinal);
        StringAssert.Contains(docContent, descriptor.Description.ToString().ReplaceLineEndings("\n"), $"Documentation should contain the diagnostic description in {docPath}", StringComparison.Ordinal);
    }

    private static IEnumerable<(DiagnosticDescriptor Descriptor, string Name)> GetAllDiagnosticDescriptors()
    {
        // Get DataPoint diagnostics
        var dataPointType = typeof(DiagnosticRules.DataPoint);
        foreach (var field in dataPointType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            if (field.FieldType == typeof(DiagnosticDescriptor) && field.IsInitOnly)
                yield return ((DiagnosticDescriptor)field.GetValue(null)!, field.Name.Replace("Attribute", string.Empty, StringComparison.Ordinal));
        }

        // Get GodotEngine diagnostics
        var godotEngineType = typeof(DiagnosticRules.GodotEngine);
        foreach (var field in godotEngineType.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
        {
            if (field.FieldType == typeof(DiagnosticDescriptor) && field.IsInitOnly)
                yield return ((DiagnosticDescriptor)field.GetValue(null)!, field.Name);
        }
    }

    private static List<DiagnosticRuleTableRecord> ParseMarkdownTable(List<string> lines)
    {
        var tableRows = new List<DiagnosticRuleTableRecord>();
        var rowRegex = IndexTableRowRegex();

        foreach (var line in lines)
        {
            var match = rowRegex.Match(line);
            if (match.Success)
            {
                tableRows.Add(
                    new DiagnosticRuleTableRecord
                    {
                        Id = match.Groups[1].Value.Trim(),
                        Severity = match.Groups[2].Value.Trim(),
                        Title = match.Groups[3].Value.Trim()
                    });
            }
        }

        return tableRows;
    }

    [GeneratedRegex(@"\|\s*([^\|]+)\s*\|\s*([^\|]+)\s*\|\s*([^\|]+)\s*\|")]
    private static partial Regex IndexTableRowRegex();

    private sealed record DiagnosticRuleTableRecord
    {
        public required string Id { get; init; }

        public required string Severity { get; init; }

        public required string Title { get; init; }

        public override string ToString() => $"| {Id} | {Severity} | {Title} |";
    }
}
