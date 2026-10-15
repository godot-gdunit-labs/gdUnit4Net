namespace GdUnit4.Tests.Core.Discovery;

using System;
using System.Collections.Generic;
using System.Text.Json;

using GdUnit4.Core.Discovery;

using static Assertions;

[TestSuite]
public class TestCaseDescriptorTest
{
    [TestCase]
    public void IsEqual()
    {
        var guid = Guid.NewGuid();
        var dsA = new TestCaseDescriptor
        {
            SimpleName = "TestA",
            FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.TestA",
            AssemblyPath = "/path/to/test_assembly.dll",
            ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
            ManagedMethod = "SingleTestCaseWithCustomName",
            Id = guid,
            LineNumber = 29,
            CodeFilePath = "d:/projectX/tests/core/discovery/ExampleTestSuiteToDiscover.cs",
            AttributeIndex = 0,
            RequireRunningGodotEngine = false,
            Categories = new List<string>
            {
                "CategoryA",
                "Foo"
            },
            Traits = new Dictionary<string, List<string>> { ["Category"] = ["Foo"] }
        };

        var dsB = new TestCaseDescriptor
        {
            SimpleName = "TestA",
            FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.TestA",
            AssemblyPath = "/path/to/test_assembly.dll",
            ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
            ManagedMethod = "SingleTestCaseWithCustomName",
            Id = guid,
            LineNumber = 29,
            CodeFilePath = "d:/projectX/tests/core/discovery/ExampleTestSuiteToDiscover.cs",
            AttributeIndex = 0,
            RequireRunningGodotEngine = false,
            Categories = new List<string>
            {
                "CategoryA",
                "Foo"
            },
            Traits = new Dictionary<string, List<string>> { ["Category"] = ["Foo"] }
        };

        AssertBool(dsA.Equals(dsB)).IsTrue();
    }

    [TestCase]
    public void ToStringWritesIndentedJson()
    {
        var guid = Guid.NewGuid();
        var descriptor = new TestCaseDescriptor
        {
            SimpleName = "TestA",
            FullyQualifiedName = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover.TestA",
            AssemblyPath = "/path/to/test_assembly.dll",
            ManagedType = "GdUnit4.Tests.Core.Discovery.ExampleTestSuiteToDiscover",
            ManagedMethod = "SingleTestCaseWithCustomName",
            Id = guid,
            LineNumber = 29,
            CodeFilePath = null,
            AttributeIndex = 0,
            RequireRunningGodotEngine = true,
            Categories = ["CategoryA"],
            Traits = new Dictionary<string, List<string>> { ["Category"] = ["Foo"] }
        };

        var json = descriptor.ToString();

        AssertString(json).Contains($"{{{Environment.NewLine}  \"SimpleName\": \"TestA\",");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        AssertThat(root.GetProperty("Id").GetGuid()).IsEqual(guid);
        AssertThat(root.GetProperty("LineNumber").GetInt32()).IsEqual(29);
        AssertThat(root.GetProperty("CodeFilePath").ValueKind).IsEqual(JsonValueKind.Null);
        AssertThat(root.GetProperty("RequireRunningGodotEngine").GetBoolean()).IsTrue();
        AssertThat(root.GetProperty("Categories")[0].GetString()).IsEqual("CategoryA");
        AssertThat(root.GetProperty("Traits").GetProperty("Category")[0].GetString()).IsEqual("Foo");
    }
}
