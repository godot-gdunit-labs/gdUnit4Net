// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core;

using System.Diagnostics.CodeAnalysis;

using Godot;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

internal static class GdUnitTestSuiteBuilder
{
    private const string DEFAULT_TEMP_TS_CS = """
                                                  // GdUnit generated TestSuite

                                                  using Godot;
                                                  using GdUnit4;

                                                  namespace ${name_space}
                                                  {
                                                      using static Assertions;
                                                      using static Utils;

                                                      [TestSuite]
                                                      public class ${suite_class_name}
                                                      {
                                                          // TestSuite generated from
                                                          private const string sourceClazzPath = "${source_resource_path}";

                                                      }
                                                  }
                                              """;

    private const string TAG_TEST_SUITE_NAMESPACE = "${name_space}";
    private const string TAG_TEST_SUITE_CLASS = "${suite_class_name}";
    private const string TAG_SOURCE_CLASS_NAME = "${source_class}";
    private const string TAG_SOURCE_CLASS_VARNAME = "${source_var}";
    private const string TAG_SOURCE_RESOURCE_PATH = "${source_resource_path}";

    private static readonly Dictionary<string, Type> ClazzCache = [];

    public static Dictionary<string, object> Build(string sourcePath, int lineNumber, string testSuitePath)
    {
        var result = new Dictionary<string, object> { { "path", testSuitePath } };
        try
        {
            var classDefinition = ParseFullqualifiedClassName(sourcePath, lineNumber);
            if (classDefinition == null)
            {
                result.Add("error", $"Can't find a class, struct, interface or record in {sourcePath}:{lineNumber}.");
                return result;
            }

            var methodToTest = FindMethod(sourcePath, lineNumber);
            if (string.IsNullOrEmpty(methodToTest))
            {
                result.Add("error", $"Can't parse method name from {sourcePath}:{lineNumber}.");
                return result;
            }

            // create a directory if not exists
            var dir = Path.GetDirectoryName(testSuitePath);
            if (!Directory.Exists(dir))
                _ = Directory.CreateDirectory(dir!);

            if (!File.Exists(testSuitePath))
            {
                var template = FillFromTemplate(LoadTestSuiteTemplate(), classDefinition, sourcePath);
                var syntaxTree = CSharpSyntaxTree.ParseText(template);
                var toWrite = AddTestCase(syntaxTree, methodToTest);

                using (var streamWriter = File.CreateText(testSuitePath))
                    toWrite.WriteTo(streamWriter);
                result.Add("line", TestCaseLineNumber(toWrite, methodToTest));
            }
            else
            {
                var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(testSuitePath));
                var toWrite = syntaxTree.WithFilePath(testSuitePath).GetCompilationUnitRoot();
                if (TestCaseExists(toWrite, methodToTest))
                {
                    result.Add("line", TestCaseLineNumber(toWrite, methodToTest));
                    return result;
                }

                toWrite = AddTestCase(syntaxTree, methodToTest);
                using (var streamWriter = File.CreateText(testSuitePath))
                    toWrite.WriteTo(streamWriter);
                result.Add("line", TestCaseLineNumber(toWrite, methodToTest));
            }

            return result;
        }

        // a line number outside the source file, a template without a class, and a test suite path that cannot be written are reported to the editor
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            Console.Error.WriteLine($"Can't create the test suite for {sourcePath}:{lineNumber}. Error: {e.Message}");
            result.Add("error", e.Message);
            return result;
        }
    }

    /// <summary>
    ///     Parses the type definition of a source file, the type containing the given line or the first type of the file.
    /// </summary>
    /// <param name="classPath">The path of the source file.</param>
    /// <param name="lineNumber">The line number to find the containing type for, 0 selects the first type.</param>
    /// <returns>The type definition, or null when the file cannot be read or declares no class, struct, interface or record.</returns>
    internal static ClassDefinition? ParseFullqualifiedClassName(string classPath, int lineNumber = 0)
    {
        if (string.IsNullOrEmpty(classPath) || !new FileInfo(classPath).Exists)
        {
            Console.Error.WriteLine($"Class `{classPath}` does not exist.");
            return null;
        }

        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(classPath));
            var lines = syntaxTree.GetText().Lines;
            TextSpan? spanToFind = lineNumber > 0 && lineNumber <= lines.Count ? lines[lineNumber - 1].Span : null;
            var typeSyntax = TypeDeclaration(syntaxTree.GetCompilationUnitRoot(), spanToFind);
            return typeSyntax == null ? null : new ClassDefinition(NamespaceOf(typeSyntax), typeSyntax.Identifier.ValueText);
        }

        // a source file that cannot be read has no type definition
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"Can't read the source file {classPath}. Error: {e.Message}");
            return null;
        }
    }

    internal static Type? ParseType(string classPath, bool isTestSuite = false)
    {
        if (string.IsNullOrEmpty(classPath) || !new FileInfo(classPath).Exists)
        {
            Console.WriteLine($"Warning: Class `{classPath}` does not exist.");
            return null;
        }

        return FindClassWithTestSuiteAttribute(classPath, isTestSuite);
    }

    internal static int TestCaseLineNumber(CompilationUnitSyntax root, string testCaseName)
    {
        var classDeclaration = ClassDeclaration(root);

        // lookup on test cases
        var method = classDeclaration.Members.OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(m => m.Identifier.Text.Equals(testCaseName, StringComparison.Ordinal));
        if (method?.Body != null)
            return method.Body.GetLocation().GetLineSpan().StartLinePosition.Line;

        // If the method has not a body, return the line of the method declaration
        return (method?.Identifier.GetLocation().GetLineSpan().StartLinePosition.Line + 1) ?? -1;
    }

    internal static string? FindMethod(string sourcePath, int lineNumber)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(sourcePath));
        var spanToFind = syntaxTree.GetText().Lines[lineNumber - 1].Span;
        var typeSyntax = TypeDeclaration(syntaxTree.GetCompilationUnitRoot(), spanToFind);
        if (typeSyntax == null)
            return null;

        // lookup on properties
        foreach (var m in typeSyntax.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (m.FullSpan.IntersectsWith(spanToFind))
                return m.Identifier.Text;
        }

        // lookup on methods
        foreach (var m in typeSyntax.Members.OfType<MethodDeclarationSyntax>())
        {
            if (m.FullSpan.IntersectsWith(spanToFind))
                return m.Identifier.Text;
        }

        return null;
    }

    /// <summary>
    ///     Finds the innermost class, struct, interface or record containing the span, or the first one of the file without a span.
    /// </summary>
    private static TypeDeclarationSyntax? TypeDeclaration(CompilationUnitSyntax root, TextSpan? spanToFind)
    {
        // the nodes are ordered by their position, so a nested type follows the type that contains it
        var types = root.DescendantNodes().OfType<TypeDeclarationSyntax>().ToList();
        if (spanToFind == null)
            return types.FirstOrDefault();
        return types.LastOrDefault(type => type.FullSpan.IntersectsWith(spanToFind.Value)) ?? types.FirstOrDefault();
    }

    private static string? NamespaceOf(TypeDeclarationSyntax typeSyntax)
    {
        var names = typeSyntax.Ancestors()
            .OfType<BaseNamespaceDeclarationSyntax>()
            .Reverse()
            .Select(namespaceSyntax => namespaceSyntax.Name.ToString())
            .ToList();
        return names.Count == 0 ? null : string.Join('.', names);
    }

    private static Type? FindClassWithTestSuiteAttribute(string classPath, bool isTestSuite)
    {
        var code = File.ReadAllText(classPath);
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetCompilationUnitRoot();
        var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .FirstOrDefault(classDeclaration =>
            {
                var hasTestSuiteAttribute = classDeclaration.AttributeLists
                    .SelectMany(attrList => attrList.Attributes)
                    .Any(attribute => attribute.Name.ToString() == "TestSuite");
                return isTestSuite ? hasTestSuiteAttribute : !hasTestSuiteAttribute;
            });

        if (classDeclaration != null)
        {
            // Construct a full class name with namespace
            var namespaceSyntax = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().FirstOrDefault()
                                  ?? classDeclaration.Ancestors().OfType<FileScopedNamespaceDeclarationSyntax>().FirstOrDefault() as BaseNamespaceDeclarationSyntax;
            var className = namespaceSyntax != null ? namespaceSyntax.Name + "." + classDeclaration.Identifier : classDeclaration.Identifier.ValueText;
            return FindTypeOnAssembly(className);
        }

        Console.WriteLine($"Warning: No class found in the provided code ({classPath}).");
        return null;
    }

    private static Type? FindTypeOnAssembly(string clazz)
    {
        if (ClazzCache.TryGetValue(clazz, out var onAssembly))
            return onAssembly;
        var type = Type.GetType(clazz);
        if (type != null)
            return type;

        // if the class isn't found on current assembly lookup over all other loaded assemblies
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(clazz);
            if (type != null)
            {
                ClazzCache.Add(clazz, type);
                return type;
            }
        }

        return null;
    }

    private static string LoadTestSuiteTemplate()
    {
        if (ProjectSettings.HasSetting("gdunit4/templates/testsuite/CSharpScript"))
            return (string)ProjectSettings.GetSetting("gdunit4/templates/testsuite/CSharpScript");
        return DEFAULT_TEMP_TS_CS;
    }

    [SuppressMessage("Globalization", "CA1307", Justification = "This is a template and not a user input.")]
    private static string FillFromTemplate(string template, ClassDefinition classDefinition, string classPath) =>
        template
            .Replace(TAG_TEST_SUITE_NAMESPACE, string.IsNullOrEmpty(classDefinition.Namespace) ? "GdUnitDefaultTestNamespace" : classDefinition.Namespace)
            .Replace(TAG_TEST_SUITE_CLASS, classDefinition.Name + "Test")
            .Replace(TAG_SOURCE_RESOURCE_PATH, classPath)
            .Replace(TAG_SOURCE_CLASS_NAME, classDefinition.Name)
            .Replace(TAG_SOURCE_CLASS_VARNAME, classDefinition.Name);

    // the test suite is the first class of its file, inside or outside a namespace
    private static ClassDeclarationSyntax ClassDeclaration(CompilationUnitSyntax root)
        => root.DescendantNodes().OfType<ClassDeclarationSyntax>().First();

    private static bool TestCaseExists(CompilationUnitSyntax root, string testCaseName) =>
        ClassDeclaration(root).Members.OfType<MethodDeclarationSyntax>()
            .Any(method => method.Identifier.Text.Equals(testCaseName, StringComparison.Ordinal));

    private static CompilationUnitSyntax AddTestCase(SyntaxTree syntaxTree, string testCaseName)
    {
        var root = syntaxTree.GetCompilationUnitRoot();
        var programClassSyntax = ClassDeclaration(root);
        var insertAt = programClassSyntax.ChildNodes().Last();

        var testCaseAttribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("TestCase"));
        var attributes = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(testCaseAttribute));

        var method = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.List<AttributeListSyntax>().Add(attributes),
            SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
            SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            default,
            SyntaxFactory.Identifier(testCaseName),
            default,
            SyntaxFactory.ParameterList(),
            default,
            SyntaxFactory.Block(),
            default,
            default);

        var newBody = SyntaxFactory.Block(SyntaxFactory.ParseStatement("AssertNotYetImplemented();"));
        method = method.ReplaceNode(method.Body!, newBody);
        return root.InsertNodesAfter(insertAt, [method]).NormalizeWhitespace("\t", "\n");
    }

    internal class ClassDefinition : IEquatable<object>
    {
        public ClassDefinition(string? nameSpace, string name)
        {
            Namespace = nameSpace;
            Name = name;
        }

        public string? Namespace { get; }

        public string Name { get; }

        public string ClassName => Namespace == null ? Name : $"{Namespace}.{Name}";

        public override bool Equals(object? obj)
            => obj is ClassDefinition definition &&
               Namespace == definition.Namespace &&
               Name == definition.Name;

        public override int GetHashCode() => HashCode.Combine(Namespace, Name, ClassName);
    }
}
