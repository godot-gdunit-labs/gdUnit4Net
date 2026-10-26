# The C# GdUnit4 API

## What is GdUnit4.Api

gdUnit4.api is the C# package to enable GdUnit4 to run/write unit tests in C#.

## Features

* **Writing And Executing Tests** in C# for Net8.0 and Net10.0 C#14
* **Convenient interface** for running test-suites directly from Godot<br>
  One of the main features of GdUnit4 is the ability to run test-suites directly from the Godot editor using the context menu. You can run test-suites from the
  FileSystem panel, the ScriptEditor, or the GdUnit Inspector. Right-click the desired test-suite or test-case and select "Run Test(s)" to run tests and display
  the results in the GdUnit Inspector.<br>
  Create new test cases from the ScriptEditor by right-clicking on the function you want to test and selecting "Create TestCase" from the context menu.
* **Fluent Syntax** for writing test cases that's easy to read and understand
* **Wide range of Assertions** for verifying the behavior and output of your code
* **Test Fuzzing support:** for generating random inputs to test edge cases and boundary conditions
* **Parameterized Tests:** (Test Cases) for testing functions with multiple sets of inputs and expected outputs
* **Dynamic Tests Data:** 'DataPoint' attribute to define test data sets
* **Scene runner:** for simulating different kinds of inputs and actions, such as mouse clicks and keyboard inputs<br>
  For example, you can simulate mouse clicks and keyboard inputs by calling the appropriate methods on the runner instance. You can also wait for a specific
  signal to be emitted by the scene or wait for a specific function to return a certain value.
* **Integration with Test Adapter:** Works seamlessly with the [gdUnit4.test.adapter](../TestAdapter/README.md) for running tests in Visual Studio, VS Code,
  and JetBrains Rider
* **Multi-project testing (`5.1.0-rc7`):** A separate test project can reference a Godot game project and load its resources, scenes, scripts, and settings at
  runtime. See the [multi-project guide](https://github.com/godot-gdunit-labs/gdUnit4Net/blob/master/Api/doc/multi-project.md).

## Installation

You can install the GdUnit4 API by adding it as a package reference to your project:

```xml
<PackageReference Include="gdUnit4.api" Version="5.1.0-rc7"/>
```

## Related Packages

* [gdUnit4.test.adapter](../TestAdapter/README.md) - Run your tests in Visual Studio, VS Code, and JetBrains Rider
* [gdUnit4.analyzers](../Analyzers/README.md) - Add compile-time validation for your test code

## Short Example

```csharp
namespace GdUnit4.Tests
{
    using static Assertions;

    [TestSuite]
    public class StringAssertTest
    {
        [TestCase]
        public void IsEqual()
        {
            AssertThat("This is a test message").IsEqual("This is a test message");
        }
        
        [TestCase] 
        [RequireGodotRuntime] // ← Add this for Godot-dependent tests
        public void IsEqual()
        {
             AssertThat(new Node2D()).IsNotNull();
        }
        
        [Test]
        [RequireGodotRuntime]
        [GodotExceptionMonitor]  // ← Monitor Godot exceptions
        public void TestNodeCallback()
        {
            var node = new MyNode(); // Will catch exceptions in _Ready()
            AddChild(node);
        }
        
        [Test]
        [DataPoint(nameof(TestData))]  // ← Data-driven tests
        public void TestCalculations(int a, int b, int expected)
        {
            AssertThat(Calculator.Add(a, b)).IsEqual(expected);
        }
        
        [Test]
        [ThrowsException(typeof(ArgumentNullException), "Value cannot be null")]
        public void TestValidation()
        {
            Calculator.Add(null, 5); // Expects specific exception
        }
        
        // Data source for parameterized tests
        public static IEnumerable<object[]> TestData => new[]
        {
            new object[] { 1, 2, 3 },
            new object[] { 5, 7, 12 }
        };
    }
 }
```

## Documentation

For more information, check out the [complete API documentation](https://mikeschulze.github.io/gdUnit4/).

---

### You are welcome to

* [Give Feedback](https://github.com/MikeSchulze/gdUnit4Net/discussions)
* [Suggest Improvements](https://github.com/MikeSchulze/gdUnit4Net/issues/new?assignees=MikeSchulze&labels=enhancement&template=feature_request.md&title=)
* [Report Bugs](https://github.com/MikeSchulze/gdUnit4Net/issues/new?assignees=MikeSchulze&labels=bug%2C+task&template=bug_report.md&title=)

### Thank you for supporting my project

---

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.
