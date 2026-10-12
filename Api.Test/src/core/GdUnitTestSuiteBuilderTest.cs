namespace GdUnit4.Tests.Core;

using System.IO;
using System.Text;

using GdUnit4.Core;

using Godot;

using SpaceA;

using Tests.Resources;

using static Assertions;

using static Utils;

[TestSuite]
public class GdUnitTestSuiteBuilderTest
{
    [AfterTest]
    public void AfterEach()
        => ClearTempDir();

    [TestCase]
    public void ParseFullqualifiedClassName()
        => AssertThat(GdUnitTestSuiteBuilder.ParseFullqualifiedClassName("src/core/resources/sources/TestPerson.cs"))
            .IsEqual(new GdUnitTestSuiteBuilder.ClassDefinition("GdUnit4.Example.Test.Resources", "TestPerson"));

    [TestCase]
    public void ParseTypeWithNamespace()
    {
        AssertObject(GdUnitTestSuiteBuilder.ParseType("src/core/resources/testsuites/mono/spaceA/TestSuite.cs")).IsEqual(typeof(TestSuite));
        AssertObject(GdUnitTestSuiteBuilder.ParseType("src/core/resources/testsuites/mono/spaceB/TestSuite.cs")).IsEqual(typeof(SpaceB.TestSuite));

        // source file doesn't exist
        AssertObject(GdUnitTestSuiteBuilder.ParseType("src/core/resources/testsuites/mono/spaceC/TestSuite.cs")).IsNull();
    }

    [TestCase]
    public void ParseTypeWithoutNamespace()
        => AssertObject(GdUnitTestSuiteBuilder.ParseType("src/core/resources/testsuites/mono/noSpace/TestSuiteWithoutNamespace.cs"))
            .IsEqual(typeof(TestSuiteWithoutNamespace));

    [TestCase]
    public void ParseTypeWithFileScopedNamespace()
        => AssertObject(GdUnitTestSuiteBuilder.ParseType("src/core/resources/testsuites/mono/TestSuiteWithFileScopedNamespace.cs"))
            .IsEqual(typeof(TestSuiteWithFileScopedNamespace));

    [TestCase]
    [RequireGodotRuntime]
    public void FindMethodLineOutOfRange()
    {
        var classPath = ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson.cs");
        AssertThrown(() => GdUnitTestSuiteBuilder.FindMethod(classPath, 0))
            .StartsWithMessage("Specified argument was out of the range of valid values.");
        AssertThrown(() => GdUnitTestSuiteBuilder.FindMethod(classPath, 10000))
            .StartsWithMessage("Specified argument was out of the range of valid values.");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void FindMethodNoMethodFound()
    {
        var classPath = ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson.cs");
        AssertString(GdUnitTestSuiteBuilder.FindMethod(classPath, 5)).IsNull();
        AssertString(GdUnitTestSuiteBuilder.FindMethod(classPath, 9)).IsNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void FindMethodFound()
    {
        var classPath = ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson.cs");
        AssertString(GdUnitTestSuiteBuilder.FindMethod(classPath, 12)).IsEqual("FirstName");
        AssertString(GdUnitTestSuiteBuilder.FindMethod(classPath, 14)).IsEqual("LastName");
        AssertString(GdUnitTestSuiteBuilder.FindMethod(classPath, 16)).IsEqual("FullName");
        AssertString(GdUnitTestSuiteBuilder.FindMethod(classPath, 18)).IsEqual("FullName2");
        AssertString(GdUnitTestSuiteBuilder.FindMethod(classPath, 20)).IsEqual("FullName3");
        AssertString(GdUnitTestSuiteBuilder.FindMethod(classPath, 21)).IsEqual("FullName3");
        AssertString(GdUnitTestSuiteBuilder.FindMethod(classPath, 22)).IsEqual("FullName3");
        AssertString(GdUnitTestSuiteBuilder.FindMethod(classPath, 23)).IsEqual("FullName3");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CreateTestSuite()
    {
        var tmp = CreateTempDir("build-test-suite-test");
        var sourceClass = Path.Combine(tmp, "TestPerson.cs");
        File.Copy(Path.GetFullPath(ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson.cs")), sourceClass);

        // first time generates the test suite and adds the test case
        var testSuite = Path.Combine(tmp, "TestPersonTest.cs");
        var dictionary = GdUnitTestSuiteBuilder.Build(sourceClass, 24, testSuite);
        AssertThat(dictionary["path"]).IsEqual(testSuite);
        AssertThat((int)dictionary["line"]).IsEqual(16);
        AssertThat(File.ReadAllText(testSuite, Encoding.UTF8)).IsEqual(NewCreatedTestSuite(sourceClass));

        // the second call updated the existing test suite and added a new test case
        dictionary = GdUnitTestSuiteBuilder.Build(sourceClass, 14, testSuite);
        AssertThat(dictionary["path"]).IsEqual(testSuite);
        AssertThat((int)dictionary["line"]).IsEqual(22);
        AssertThat(File.ReadAllText(testSuite, Encoding.UTF8)).IsEqual(UpdatedTestSuite(sourceClass));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CreateTestSuiteNoMethodFound()
    {
        var tmp = CreateTempDir("build-test-suite-test");
        var sourceClass = Path.Combine(tmp, "TestPerson.cs");
        File.Copy(Path.GetFullPath(ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson.cs")), sourceClass);

        // use of a line number for which no method is defined in the source class
        var dictionary = GdUnitTestSuiteBuilder.Build(sourceClass, 4, Path.Combine(tmp, "TestPersonTest.cs"));
        AssertThat((string)dictionary["error"])
            .StartsWith("Can't parse method name from")
            .EndsWith("TestPerson.cs:4.");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CreateTestSuiteNoNamespace()
    {
        var tmp = CreateTempDir("build-test-suite-test");
        var sourceClass = Path.Combine(tmp, "TestPerson2.cs");
        File.Copy(Path.GetFullPath(ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson2.cs")), sourceClass);

        // use of a line number for which no method is defined in the source class
        var testSuite = Path.Combine(tmp, "TestPerson2Test.cs");
        var dictionary = GdUnitTestSuiteBuilder.Build(sourceClass, 12, testSuite);
        AssertThat(dictionary["path"]).IsEqual(testSuite);
        AssertThat((int)dictionary["line"]).IsEqual(16);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CreateTestSuiteWithNamespace()
    {
        var tmp = CreateTempDir("build-test-suite-test");
        var sourceClass = Path.Combine(tmp, "TestPerson.cs");
        File.Copy(Path.GetFullPath(ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson.cs")), sourceClass);

        // use of a line number for which no method is defined in the source class
        var testSuite = Path.Combine(tmp, "TestPersonTest.cs");
        var dictionary = GdUnitTestSuiteBuilder.Build(sourceClass, 14, testSuite);
        AssertThat(dictionary["path"]).IsEqual(testSuite);
        AssertThat((int)dictionary["line"]).IsEqual(16);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void CreateTestSuiteTestCaseAlreadyExists()
    {
        var tmp = CreateTempDir("build-test-suite-test");
        var sourceClass = Path.Combine(tmp, "TestPerson.cs");
        File.Copy(Path.GetFullPath(ProjectSettings.GlobalizePath("res://src/core/resources/sources/TestPerson.cs")), sourceClass);

        var expected = NewCreatedTestSuite(sourceClass);

        // first time generates the test suite and adds the test case
        var testSuite = Path.Combine(tmp, "TestPersonTest.cs");
        var dictionary = GdUnitTestSuiteBuilder.Build(sourceClass, 24, testSuite);
        AssertThat(dictionary["path"]).IsEqual(testSuite);
        AssertThat((int)dictionary["line"]).IsEqual(16);
        AssertThat(File.ReadAllText(testSuite, Encoding.UTF8)).IsEqual(expected);

        // try to add again the same test case
        dictionary = GdUnitTestSuiteBuilder.Build(sourceClass, 24, testSuite);
        AssertThat(dictionary["path"]).IsEqual(testSuite);
        AssertThat((int)dictionary["line"]).IsEqual(16);

        // we expect that the contents of the test-suite will not be changed
        AssertThat(File.ReadAllText(testSuite, Encoding.UTF8)).IsEqual(expected);
    }

    private static string UpdatedTestSuite(string sourceClass) =>
        """
            // GdUnit generated TestSuite
            using Godot;
            using GdUnit4;

            namespace GdUnit4.Example.Test.Resources
            {
            	using static Assertions;
            	using static Utils;

            	[TestSuite]
            	public class TestPersonTest
            	{
            		// TestSuite generated from
            		private const string sourceClazzPath = ${sourceClazzPath};
            		[TestCase]
            		public void FullName3()
            		{
            			AssertNotYetImplemented();
            		}

            		[TestCase]
            		public void LastName()
            		{
            			AssertNotYetImplemented();
            		}
            	}
            }
            """.Replace("${sourceClazzPath}", $"\"{sourceClass}\"").Replace("\r", string.Empty);

    private static string NewCreatedTestSuite(string sourceClass) =>
        """
            // GdUnit generated TestSuite
            using Godot;
            using GdUnit4;

            namespace GdUnit4.Example.Test.Resources
            {
            	using static Assertions;
            	using static Utils;

            	[TestSuite]
            	public class TestPersonTest
            	{
            		// TestSuite generated from
            		private const string sourceClazzPath = ${sourceClazzPath};
            		[TestCase]
            		public void FullName3()
            		{
            			AssertNotYetImplemented();
            		}
            	}
            }
            """.Replace("${sourceClazzPath}", $"\"{sourceClass}\"").Replace("\r", string.Empty);
}
