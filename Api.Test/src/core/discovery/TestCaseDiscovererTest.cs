namespace GdUnit4.Tests.Core.Discovery;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;

using GdUnit4.Core.Discovery;

using Godot;

using Mono.Cecil;
using Mono.Cecil.Cil;

using static Assertions;

public sealed class ExampleTestSuiteToDiscover
{
    [TestCase]
    public void SingleTestCase()
    {
    }

    [TestCase(TestName = "TestA")]
    [TestCategory("CategoryA")]
    [Trait("Category", "Foo")]
    [Trait("Category", "Bar")]
    public void SingleTestCaseWithCustomName()
        => AssertBool(true).IsEqual(true);

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void MultiRowTestCase(int value)
    {
    }

    [TestCase(0, TestName = "TestA")]
    [TestCase(1, TestName = "TestB")]
    [TestCase(2, TestName = "TestC")]
    public void MultiRowTestCaseWithCustomTestName(int value)
    {
    }

    [TestCase]
    public async Task ThreadedTestCase()
        => await Task.CompletedTask;

    [TestCase]
    public async Task<int> ThreadedTestCaseTyped()
    {
        await Task.CompletedTask;
        return 0;
    }

    [TestCase]
    [TestCategory("Unit")]
    [TestCategory("It")]
    public async Task<int> WithCategories()
    {
        await Task.CompletedTask;
        return 0;
    }

    [TestCase]
    [Trait("Category", "Unit")]
    [Trait("Custom", "CaseA")]
    [Trait("Custom", "CaseB")]
    public async Task<int> WithTraits()
    {
        await Task.CompletedTask;
        return 0;
    }
}

[TestSuite]
public class TestCaseDiscovererTest
{
    [TestCase]
    public void DiscoverSingleTestCase()
    {
        var codeFilePath = DiscoverTestUtils.GetSourceFilePath("src/core/discovery/TestCaseDiscovererTest.cs");
        var tests = DiscoverTests<ExampleTestSuiteToDiscover>(nameof(ExampleTestSuiteToDiscover.SingleTestCase));

        AssertThat(tests).ContainsExactly(new TestCaseDescriptor
        {
            SimpleName = "SingleTestCase",
            FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.SingleTestCase",
            AssemblyPath = "/path/to/test_assembly.dll",
            ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
            ManagedMethod = "SingleTestCase",
            Id = tests[0].Id,
            LineNumber = 21,
            CodeFilePath = codeFilePath,
            AttributeIndex = 0,
            RequireRunningGodotEngine = false
        });
    }

    [TestCase]
    public void DiscoverSingleTestCaseWithCustomName()
    {
        var codeFilePath = DiscoverTestUtils.GetSourceFilePath("src/core/discovery/TestCaseDiscovererTest.cs");
        var tests = DiscoverTests<ExampleTestSuiteToDiscover>(nameof(ExampleTestSuiteToDiscover.SingleTestCaseWithCustomName));

        AssertThat(tests)
            .ContainsExactly(new TestCaseDescriptor
            {
                SimpleName = "TestA",
                FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.TestA",
                AssemblyPath = "/path/to/test_assembly.dll",
                ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
                ManagedMethod = "SingleTestCaseWithCustomName",
                Id = tests[0].Id,
                LineNumber = 29,
                CodeFilePath = codeFilePath,
                AttributeIndex = 0,
                RequireRunningGodotEngine = false,
                Categories = new List<string> { "CategoryA" },
                Traits = new Dictionary<string, List<string>>
                {
                    ["Category"] =
                    [
                        "Foo",
                        "Bar"
                    ]
                }
            });
    }

    [TestCase]
    public void DiscoverMultiRowTestCase()
    {
        var codeFilePath = DiscoverTestUtils.GetSourceFilePath("src/core/discovery/TestCaseDiscovererTest.cs");
        var tests = DiscoverTests<ExampleTestSuiteToDiscover>(nameof(ExampleTestSuiteToDiscover.MultiRowTestCase));

        AssertThat(tests)
            .HasSize(3)
            .ContainsExactly(
                new TestCaseDescriptor
                {
                    SimpleName = "MultiRowTestCase:0 (0)",
                    FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.MultiRowTestCase.MultiRowTestCase:0 (0)",
                    AssemblyPath = "/path/to/test_assembly.dll",
                    ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
                    ManagedMethod = "MultiRowTestCase",
                    Id = tests[0].Id,
                    LineNumber = 35,
                    CodeFilePath = codeFilePath,
                    AttributeIndex = 0,
                    RequireRunningGodotEngine = false
                },
                new TestCaseDescriptor
                {
                    SimpleName = "MultiRowTestCase:1 (1)",
                    FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.MultiRowTestCase.MultiRowTestCase:1 (1)",
                    AssemblyPath = "/path/to/test_assembly.dll",
                    ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
                    ManagedMethod = "MultiRowTestCase",
                    Id = tests[1].Id,
                    LineNumber = 35,
                    CodeFilePath = codeFilePath,
                    AttributeIndex = 1,
                    RequireRunningGodotEngine = false
                },
                new TestCaseDescriptor
                {
                    SimpleName = "MultiRowTestCase:2 (2)",
                    FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.MultiRowTestCase.MultiRowTestCase:2 (2)",
                    AssemblyPath = "/path/to/test_assembly.dll",
                    ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
                    ManagedMethod = "MultiRowTestCase",
                    Id = tests[2].Id,
                    LineNumber = 35,
                    CodeFilePath = codeFilePath,
                    AttributeIndex = 2,
                    RequireRunningGodotEngine = false
                });
    }

    [TestCase]
    public void DiscoverMultiRowTestCaseWithCustomNames()
    {
        var codeFilePath = DiscoverTestUtils.GetSourceFilePath("src/core/discovery/TestCaseDiscovererTest.cs");
        var tests = DiscoverTests<ExampleTestSuiteToDiscover>(nameof(ExampleTestSuiteToDiscover.MultiRowTestCaseWithCustomTestName));

        AssertThat(tests)
            .HasSize(3)
            .ContainsExactly(
                new TestCaseDescriptor
                {
                    SimpleName = "TestA:0 (0)",
                    FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.MultiRowTestCaseWithCustomTestName.TestA:0 (0)",
                    AssemblyPath = "/path/to/test_assembly.dll",
                    ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
                    ManagedMethod = "MultiRowTestCaseWithCustomTestName",
                    Id = tests[0].Id,
                    LineNumber = 42,
                    CodeFilePath = codeFilePath,
                    AttributeIndex = 0,
                    RequireRunningGodotEngine = false
                },
                new TestCaseDescriptor
                {
                    SimpleName = "TestB:1 (1)",
                    FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.MultiRowTestCaseWithCustomTestName.TestB:1 (1)",
                    AssemblyPath = "/path/to/test_assembly.dll",
                    ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
                    ManagedMethod = "MultiRowTestCaseWithCustomTestName",
                    Id = tests[1].Id,
                    LineNumber = 42,
                    CodeFilePath = codeFilePath,
                    AttributeIndex = 1,
                    RequireRunningGodotEngine = false
                },
                new TestCaseDescriptor
                {
                    SimpleName = "TestC:2 (2)",
                    FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.MultiRowTestCaseWithCustomTestName.TestC:2 (2)",
                    AssemblyPath = "/path/to/test_assembly.dll",
                    ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
                    ManagedMethod = "MultiRowTestCaseWithCustomTestName",
                    Id = tests[2].Id,
                    LineNumber = 42,
                    CodeFilePath = codeFilePath,
                    AttributeIndex = 2,
                    RequireRunningGodotEngine = false
                });
    }

    [TestCase]
    public void DiscoverThreadedTestCase()
    {
        var codeFilePath = DiscoverTestUtils.GetSourceFilePath("src/core/discovery/TestCaseDiscovererTest.cs");
        var tests = DiscoverTests<ExampleTestSuiteToDiscover>(nameof(ExampleTestSuiteToDiscover.ThreadedTestCase));

        AssertThat(tests).ContainsExactly(new TestCaseDescriptor
        {
            SimpleName = "ThreadedTestCase",
            FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.ThreadedTestCase",
            AssemblyPath = "/path/to/test_assembly.dll",
            ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
            ManagedMethod = "ThreadedTestCase",
            Id = tests[0].Id,
            LineNumber = 47,
            CodeFilePath = codeFilePath,
            AttributeIndex = 0,
            RequireRunningGodotEngine = false
        });
    }

    [TestCase]
    public void DiscoverThreadedTestCaseTyped()
    {
        var codeFilePath = DiscoverTestUtils.GetSourceFilePath("src/core/discovery/TestCaseDiscovererTest.cs");
        var tests = DiscoverTests<ExampleTestSuiteToDiscover>(nameof(ExampleTestSuiteToDiscover.ThreadedTestCaseTyped));

        AssertThat(tests).ContainsExactly(new TestCaseDescriptor
        {
            SimpleName = "ThreadedTestCaseTyped",
            FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.ThreadedTestCaseTyped",
            AssemblyPath = "/path/to/test_assembly.dll",
            ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
            ManagedMethod = "ThreadedTestCaseTyped",
            Id = tests[0].Id,
            LineNumber = 51,
            CodeFilePath = codeFilePath,
            AttributeIndex = 0,
            RequireRunningGodotEngine = false
        });
    }

    [TestCase]
    [RequireGodotRuntime]
    public void DiscoverTestCasesFromScript()
    {
        var codeFilePath = ProjectSettings.GlobalizePath("res://src/core/ExampleTestSuite.cs");
        var projectDir = ProjectSettings.GlobalizePath("res://");
        var assemblyPath = Path.GetFullPath(Path.Combine(projectDir, ".godot", "mono", "temp", "bin", "Debug", "GdUnit4ApiTest.dll"));
        var script = GD.Load<CSharpScript>("res://src/core/ExampleTestSuite.cs");

        var tests = TestCaseDiscoverer.DiscoverTestCasesFromScript(script);
        AssertArray(tests)
            .HasSize(15)

            // Verify just exemplar for certain tests
            .Contains(
                new TestCaseDescriptor
                {
                    SimpleName = "TestFoo",
                    FullyQualifiedName = "GdUnit4.Tests.Core.ExampleTestSuite.TestFoo",
                    AssemblyPath = assemblyPath,
                    ManagedType = "GdUnit4.Tests.Core.ExampleTestSuite",
                    ManagedMethod = "TestFoo",
                    Id = tests[11].Id,
                    LineNumber = 37,
                    CodeFilePath = codeFilePath,
                    AttributeIndex = 0,
                    RequireRunningGodotEngine = false,
                    Categories = new List<string> { "CategoryA" },
                    Traits = new Dictionary<string, List<string>> { ["Category"] = ["Foo"] }
                }, new TestCaseDescriptor
                {
                    SimpleName = "TestCaseA:0 (1, 2, 3, 6)",
                    FullyQualifiedName = "GdUnit4.Tests.Core.ExampleTestSuite.TestCaseArguments.TestCaseA:0 (1, 2, 3, 6)",
                    AssemblyPath = assemblyPath,
                    ManagedType = "GdUnit4.Tests.Core.ExampleTestSuite",
                    ManagedMethod = "TestCaseArguments",
                    Id = tests[5].Id,
                    LineNumber = 59,
                    CodeFilePath = codeFilePath,
                    AttributeIndex = 0,
                    RequireRunningGodotEngine = false
                }, new TestCaseDescriptor
                {
                    SimpleName = "TestCaseB:1 (3, 4, 5, 12)",
                    FullyQualifiedName = "GdUnit4.Tests.Core.ExampleTestSuite.TestCaseArguments.TestCaseB:1 (3, 4, 5, 12)",
                    AssemblyPath = assemblyPath,
                    ManagedType = "GdUnit4.Tests.Core.ExampleTestSuite",
                    ManagedMethod = "TestCaseArguments",
                    Id = tests[6].Id,
                    LineNumber = 59,
                    CodeFilePath = codeFilePath,
                    AttributeIndex = 1,
                    RequireRunningGodotEngine = false
                });
    }

    [TestCase]
    public void DiscoverTestCasesWithCategories()
    {
        var codeFilePath = DiscoverTestUtils.GetSourceFilePath("src/core/discovery/TestCaseDiscovererTest.cs");
        var tests = DiscoverTests<ExampleTestSuiteToDiscover>(nameof(ExampleTestSuiteToDiscover.WithCategories));

        AssertThat(tests).ContainsExactly(new TestCaseDescriptor
        {
            SimpleName = "WithCategories",
            FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.WithCategories",
            AssemblyPath = "/path/to/test_assembly.dll",
            ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
            ManagedMethod = "WithCategories",
            Id = tests[0].Id,
            LineNumber = 60,
            CodeFilePath = codeFilePath,
            AttributeIndex = 0,
            RequireRunningGodotEngine = false,
            Categories = new List<string> { "Unit", "It" }
        });
    }

    [TestCase]
    public void DiscoverTestCasesWithTraits()
    {
        var codeFilePath = DiscoverTestUtils.GetSourceFilePath("src/core/discovery/TestCaseDiscovererTest.cs");
        var tests = DiscoverTests<ExampleTestSuiteToDiscover>(nameof(ExampleTestSuiteToDiscover.WithTraits));

        AssertThat(tests).ContainsExactly(new TestCaseDescriptor
        {
            SimpleName = "WithTraits",
            FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.WithTraits",
            AssemblyPath = "/path/to/test_assembly.dll",
            ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
            ManagedMethod = "WithTraits",
            Id = tests[0].Id,
            LineNumber = 70,
            CodeFilePath = codeFilePath,
            AttributeIndex = 0,
            RequireRunningGodotEngine = false,
            Categories = new List<string>(),
            Traits = new Dictionary<string, List<string>>
            {
                ["Category"] = ["Unit"],
                ["Custom"] = ["CaseA", "CaseB"]
            }
        });
    }

    private IReadOnlyList<TestCaseDescriptor> DiscoverTests<TClassType>(string testMethod)
    {
        var clazzType = typeof(TClassType);
        var readerParameters = new ReaderParameters
        {
            ReadSymbols = true,
            SymbolReaderProvider = new PortablePdbReaderProvider()
        };
        using var assemblyDefinition = AssemblyDefinition.ReadAssembly(clazzType.Module.FullyQualifiedName, readerParameters);
        var methodDefinition = DiscoverTestUtils.FindMethodDefinition(assemblyDefinition, clazzType, testMethod);
        return TestCaseDiscoverer.DiscoverTestCasesFromMethod(
            methodDefinition,
            "/path/to/test_assembly.dll",
            false,
            clazzType.FullName ?? string.Empty,
            ImmutableList.Create<string>(),
            ImmutableDictionary.Create<string, List<string>>());
    }
}
