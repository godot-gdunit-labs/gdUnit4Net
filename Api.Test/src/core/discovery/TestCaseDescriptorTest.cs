namespace GdUnit4.Tests.Core.Discovery;

using System;
using System.Collections.Generic;

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
}
