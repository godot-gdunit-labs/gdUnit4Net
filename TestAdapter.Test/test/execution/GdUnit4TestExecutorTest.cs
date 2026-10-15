namespace GdUnit4.TestAdapter.Test.Execution;

using Core.Discovery;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using TestAdapter.Settings;

using static Assertions;

[TestClass]
public class GdUnit4TestExecutorTest
{
    private const string XML_SETTINGS =
        """
        <?xml version="1.0" encoding="utf-8"?>
        <RunSettings>
            <RunConfiguration>
                <MaxCpuCount>1</MaxCpuCount>
                <TestAdaptersPaths>.</TestAdaptersPaths>
                <ResultsDirectory>./TestResults</ResultsDirectory>
                <TargetFrameworks>net7.0;net8.0</TargetFrameworks>
                <!-- set default session timeout to 5m -->
                <TestSessionTimeout>500000</TestSessionTimeout>
                <TreatNoTestsAsError>true</TreatNoTestsAsError>
                <EnvironmentVariables>
                    <TestEnvironmentA>23</TestEnvironmentA>
                    <TestEnvironmentB>42</TestEnvironmentB>
                </EnvironmentVariables>
            </RunConfiguration>
        </RunSettings>
        """;

    [TestCleanup]
    public void Cleanup()
    {
        Environment.SetEnvironmentVariable("TestEnvironmentA", null);
        Environment.SetEnvironmentVariable("TestEnvironmentB", null);
    }

    [TestMethod]
    public void SetupRunnerEnvironment()
    {
        // pretest the environment is not set
        Assert.AreEqual(Environment.GetEnvironmentVariable("TestEnvironmentA"), null);
        Environment.SetEnvironmentVariable("TestEnvironmentB", "666");

        var frameworkHandle = new Mock<IFrameworkHandle>().Object;

        // Setup mock RunContext with RunSettings
        var mockRunContext = new Mock<IRunContext>();
        _ = mockRunContext.SetupGet(rc => rc.RunSettings)
            .Returns(Mock.Of<IRunSettings>(rs => rs.SettingsXml == XML_SETTINGS));

        // run
        GdUnit4TestExecutor.SetupRunnerEnvironment(mockRunContext.Object, frameworkHandle);

        // verify the TestEnvironmentA=23 and TestEnvironmentB is overwritten by 42
        Assert.AreEqual(Environment.GetEnvironmentVariable("TestEnvironmentA"), "23");
        Assert.AreEqual(Environment.GetEnvironmentVariable("TestEnvironmentB"), "42");
    }

    [TestMethod]
    public void ToGdUnitTestNodesGroupsSuitesByManagedTypeOnUnknownSourceFile()
    {
        // test cases of an assembly built with full PDBs have no source file and line number
        List<TestCase> testCases =
        [
            BuildTestCase("Examples.ASuiteTest", "First", "unknown", -1),
            BuildTestCase("Examples.BSuiteTest", "Second", "unknown", -1),
            BuildTestCase("Examples.ASuiteTest", "Third", "unknown", -1)
        ];

        var suites = GdUnit4TestExecutor.ToGdUnitTestNodes(testCases)
            .SelectMany(assembly => assembly.Suites)
            .ToList();

        _ = AssertThat(suites)
            .Extract("ManagedType")
            .ContainsExactly("Examples.ASuiteTest", "Examples.BSuiteTest");
        _ = AssertThat(suites)
            .Extract("SourceFile")
            .ContainsExactly("unknown", "unknown");
        _ = AssertThat(suites.Single(suite => suite.ManagedType == "Examples.ASuiteTest").Tests)
            .Extract("ManagedMethod")
            .ContainsExactly("First", "Third");
        _ = AssertThat(suites.Single(suite => suite.ManagedType == "Examples.BSuiteTest").Tests)
            .Extract("ManagedMethod")
            .ContainsExactly("Second");
    }

    [TestMethod]
    public void ToGdUnitTestNodesGroupsSuitesByManagedTypeAndSourceFile()
    {
        List<TestCase> testCases =
        [
            BuildTestCase("Examples.ASuiteTest", "First", "ASuiteTest.cs", 10),
            BuildTestCase("Examples.BSuiteTest", "Second", "BSuiteTest.cs", 10),
            BuildTestCase("Examples.ASuiteTest", "Third", "ASuiteTest.cs", 20)
        ];

        var suites = GdUnit4TestExecutor.ToGdUnitTestNodes(testCases)
            .SelectMany(assembly => assembly.Suites)
            .ToList();

        _ = AssertThat(suites)
            .Extract("ManagedType")
            .ContainsExactly("Examples.ASuiteTest", "Examples.BSuiteTest");
        _ = AssertThat(suites)
            .Extract("SourceFile")
            .ContainsExactly("ASuiteTest.cs", "BSuiteTest.cs");
        _ = AssertThat(suites.Single(suite => suite.ManagedType == "Examples.ASuiteTest").Tests)
            .Extract("ManagedMethod")
            .ContainsExactly("First", "Third");
        _ = AssertThat(suites.Single(suite => suite.ManagedType == "Examples.ASuiteTest").Tests)
            .Extract("LineNumber")
            .ContainsExactly(10, 20);
    }

    private static TestCase BuildTestCase(string managedType, string managedMethod, string codeFilePath, int lineNumber)
        => GdUnit4TestDiscoverer.BuildTestCase(
            new TestCaseDescriptor
            {
                FullyQualifiedName = $"{managedType}.{managedMethod}",
                SimpleName = managedMethod,
                AssemblyPath = "Examples.dll",
                ManagedType = managedType,
                ManagedMethod = managedMethod,
                Id = Guid.NewGuid(),
                LineNumber = lineNumber,
                CodeFilePath = codeFilePath,
                AttributeIndex = 0,
                RequireRunningGodotEngine = false
            },
            new GdUnit4Settings());
}
