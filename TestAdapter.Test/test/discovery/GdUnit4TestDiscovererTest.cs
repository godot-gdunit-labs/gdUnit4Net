namespace GdUnit4.TestAdapter.Test.Discovery;

using System.Runtime.InteropServices;

using Api;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

using Moq;

[TestClass]
public class GdUnit4TestDiscovererTest
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

            <GdUnit4>
                <Parameters></Parameters>
                <DisplayName>FullyQualifiedName</DisplayName>
            </GdUnit4>
        </RunSettings>
        """;

    [TestMethod]
    public void DiscoverDoNotLoadTestAssembly()
    {
        var frameworkHandle = new Mock<IFrameworkHandle>();

        // Setup the mock to capture discovered tests
        var mockDiscoverySink = new Mock<ITestCaseDiscoverySink>();

        // Setup mock RunContext with RunSettings
        var mockRunContext = new Mock<IRunContext>();
        _ = mockRunContext.SetupGet(rc => rc.RunSettings)
            .Returns(Mock.Of<IRunSettings>(rs => rs.SettingsXml == XML_SETTINGS));

        // Check the initial state
        var assemblyPath = GetExampleAssemblyPath();
        Assert.IsFalse(IsAssemblyLoaded(assemblyPath), "Assembly should not be loaded initially");

        // Check discovery process
        new GdUnit4TestDiscoverer().DiscoverTests([assemblyPath], mockRunContext.Object, frameworkHandle.Object, mockDiscoverySink.Object);
        Assert.IsFalse(IsAssemblyLoaded(assemblyPath), "Assembly should not be loaded after DiscoverTests execution");
    }

    [TestMethod]
    public void DiscoverOnNoTestAssembly()
    {
        var frameworkHandle = new Mock<IFrameworkHandle>();
        var logMessages = new List<string>();

        _ = frameworkHandle
            .Setup(logger => logger.SendMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()))
            .Callback<TestMessageLevel, string>((level, message) => logMessages.Add($"{level}: {message}"));

        // Setup the mock to capture discovered tests
        var mockDiscoverySink = new Mock<ITestCaseDiscoverySink>();
        var discoveredTests = new List<TestCase>();
        _ = mockDiscoverySink
            .Setup(ds => ds.SendTestCase(It.IsAny<TestCase>()))
            .Callback<TestCase>(discoveredTests.Add);

        // Setup mock RunContext with RunSettings
        var mockRunContext = new Mock<IRunContext>();
        _ = mockRunContext.SetupGet(rc => rc.RunSettings)
            .Returns(Mock.Of<IRunSettings>(rs => rs.SettingsXml == XML_SETTINGS));

        // the first one should be excluded because it is 'TestAdapter' in the name
        // the second assembly do not contain any tests
        var assemblyPath = typeof(GdUnit4TestDiscovererTest).Assembly.Location;
        var discoverer = new GdUnit4TestDiscoverer();
        discoverer.DiscoverTests(["MSTest.TestAdapter.dll", assemblyPath], mockRunContext.Object, frameworkHandle.Object, mockDiscoverySink.Object);

        // Verify SendTestCase was never called
        mockDiscoverySink.Verify(ds => ds.SendTestCase(It.IsAny<TestCase>()), Times.Never());
        Assert.AreEqual(0, discoveredTests.Count, "Should not discover any tests for empty assembly");

        // Verify log messages
        // @formatter:off
        var assembly = typeof(GdUnit4TestDiscoverer).Assembly;
        var name = assembly.GetName();
        var version = name.Version;
        var bitness = Environment.Is64BitProcess ? "64-bit" : "32-bit";
        var runtime = RuntimeInformation.FrameworkDescription;
        CollectionAssert.AreEquivalent(
            new[]
            {
                $"Informational: [GdUnit4] VSTest Adapter: GdUnit4.TestAdapter {version} ({bitness} {runtime})",
                $"Informational: [GdUnit4] Running on GdUnit4 test engine version: {ITestEngine.EngineVersion()}",
                $"Informational: [GdUnit4] Discover tests from assembly: {assemblyPath}",
                "Informational: [GdUnit4] Discover tests done, no tests found."
            },
            logMessages,
            "Log messages don't match expected messages");

        // @formatter:on
        Assert.IsFalse(logMessages.Any(msg => msg.StartsWith("Error:")), "They should not contain any errors");
    }

    [TestMethod]
    public void DiscoverOnTestAssembly()
    {
        var frameworkHandle = new Mock<IFrameworkHandle>();
        var logMessages = new List<string>();

        _ = frameworkHandle
            .Setup(logger => logger.SendMessage(It.IsAny<TestMessageLevel>(), It.IsAny<string>()))
            .Callback<TestMessageLevel, string>((level, message) => logMessages.Add($"{level}: {message}"));

        // Setup the mock to capture discovered tests
        var mockDiscoverySink = new Mock<ITestCaseDiscoverySink>();
        var discoveredTests = new List<TestCase>();
        _ = mockDiscoverySink
            .Setup(ds => ds.SendTestCase(It.IsAny<TestCase>()))
            .Callback<TestCase>(discoveredTests.Add);

        // Setup mock RunContext with RunSettings
        var mockRunContext = new Mock<IRunContext>();
        _ = mockRunContext.SetupGet(rc => rc.RunSettings)
            .Returns(Mock.Of<IRunSettings>(rs => rs.SettingsXml == XML_SETTINGS));

        var assemblyPath = GetExampleAssemblyPath();

        Assert.IsTrue(File.Exists(assemblyPath), $"Can find the test assembly: '{assemblyPath}'");
        var discoverer = new GdUnit4TestDiscoverer();
        discoverer.DiscoverTests([assemblyPath], mockRunContext.Object, frameworkHandle.Object, mockDiscoverySink.Object);

        // Verify SendTestCase was never called
        mockDiscoverySink.Verify(ds => ds.SendTestCase(It.IsAny<TestCase>()), Times.Exactly(35));

        // Verify log messages
        // @formatter:off
        var assembly = typeof(GdUnit4TestDiscoverer).Assembly;
        var name = assembly.GetName();
        var version = name.Version;
        var bitness = Environment.Is64BitProcess ? "64-bit" : "32-bit";
        var runtime = RuntimeInformation.FrameworkDescription;
        CollectionAssert.AreEquivalent(
            new[]
            {
                $"Informational: [GdUnit4] VSTest Adapter: GdUnit4.TestAdapter {version} ({bitness} {runtime})",
                $"Informational: [GdUnit4] Running on GdUnit4 test engine version: {ITestEngine.EngineVersion()}",
                $"Informational: [GdUnit4] Discover tests from assembly: {assemblyPath}",
                "Informational: [GdUnit4] Discover:  TestSuite Examples.Test.ExampleTest with 6 TestCases found.",
                "Informational: [GdUnit4] Discover:  TestSuite Examples.Test.Api.AssertionsTest with 2 TestCases found.",
                "Informational: [GdUnit4] Discover:  TestSuite Examples.Test.Api.GodotTypes.AssertObjectTest with 7 TestCases found.",
                "Informational: [GdUnit4] Discover:  TestSuite Examples.Test.Api.GodotTypes.AssertThatCoverageTest with 6 TestCases found.",
                "Informational: [GdUnit4] Discover:  TestSuite Examples.Test.Api.CSharpTypes.AssertObjectTest with 7 TestCases found.",
                "Informational: [GdUnit4] Discover:  TestSuite Examples.Test.Api.CSharpTypes.AssertThatCoverageTest with 7 TestCases found.",
                "Informational: [GdUnit4] Discover tests done, 6 TestSuites and total 35 Tests found."
            },
            logMessages,
            "Log messages don't match expected messages");

        // @formatter:on
        Assert.IsFalse(logMessages.Any(msg => msg.StartsWith("Error:")), "They should not contain any errors");

        // Verify discovered tests
        Assert.AreEqual(35, discoveredTests.Count, "Should discover any tests from assembly");

        // Verify properties exemplary
        AssertTestCase(
            discoveredTests,
            "Examples.Test.ExampleTest.Success",
            "Success",
            assemblyPath,
            @"Example\test\ExampleTest.cs",
            15);

        // multi testcase attribute usage
        AssertTestCase(
            discoveredTests,
            "Examples.Test.ExampleTest.DataRows.TestA:0 (0, 1, 2)",
            "TestA:0 (0, 1, 2)",
            assemblyPath,
            @"Example\test\ExampleTest.cs",
            32);
        AssertTestCase(
            discoveredTests,
            "Examples.Test.ExampleTest.DataRows.TestB:1 (1, 2, 3)",
            "TestB:1 (1, 2, 3)",
            assemblyPath,
            @"Example\test\ExampleTest.cs",
            32);
    }

    private static bool IsAssemblyLoaded(string assemblyPath)
        => AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(a => a.Location == assemblyPath);

    private static string GetExampleAssemblyPath()
    {
        // Get test assembly location
        var testDir = Path.GetDirectoryName(typeof(GdUnit4TestDiscovererTest).Assembly.Location)!;

        // Navigate up to solution root
        var solutionRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));

        // Godot projects compile to .godot/mono/temp/bin/Debug/
        var exampleAssembly = Path.Combine(solutionRoot, "Example", ".godot", "mono", "temp", "bin", "Debug", "ExampleProject.dll");

        // Verify it exists
        if (!File.Exists(exampleAssembly))
        {
            throw new FileNotFoundException(
                $"Example assembly not found at {exampleAssembly}. " +
                "Make sure the Godot Example project is built.");
        }

        return exampleAssembly;
    }

    private void AssertTestCase(IEnumerable<TestCase> tests, string fullyQualifiedName, string displayName, string source, string codeFilePath, int lineNumber)
    {
        var test = tests.FirstOrDefault(t => t.FullyQualifiedName == fullyQualifiedName);
        Assert.IsNotNull(test, $"Test {fullyQualifiedName} not found");
        Assert.IsNotNull(test.Id);
        Assert.AreEqual(fullyQualifiedName, test.FullyQualifiedName);
        Assert.AreEqual(displayName, test.DisplayName);
        Assert.AreEqual(new Uri(GdUnit4TestExecutor.EXECUTOR_URI), test.ExecutorUri);
        Assert.AreEqual(source, test.Source);
        var expectedPath = codeFilePath.Replace('\\', Path.DirectorySeparatorChar);
        var actualPath = test.CodeFilePath?.Replace('\\', Path.DirectorySeparatorChar);
        StringAssert.EndsWith(actualPath, expectedPath, StringComparison.Ordinal);
        Assert.AreEqual(lineNumber, test.LineNumber);
    }
}
