<h2 align="center">The Unit Testing Framework in C# for Godot</h2>
<p align="center">
  <a href="https://www.patreon.com/cw/GodotGdUnitLabs">
    <img src="assets/patreon-banner.svg" alt="Stay Connected &amp; Updated — Follow on Patreon" width="100%"/>
  </a>
</p>

<p align="center">This version of GdUnit4.api is based on Godot <strong>v4.6.stable.mono.official [89cea1439]</strong> (master branch)</p>

<h1 align="center">Supported Godot Versions</h1>
<p align="center">
  <img src="https://img.shields.io/badge/Godot-v4.6.0-%23478cbf?logo=godot-engine&logoColor=cyian&color=green">
  <img src="https://img.shields.io/badge/Godot-v4.6.3-%23478cbf?logo=godot-engine&logoColor=cyian&color=green">
  <img src="https://img.shields.io/badge/Godot-v4.7.1-%23478cbf?logo=godot-engine&logoColor=cyian&color=green">
  <img src="https://img.shields.io/badge/Godot-v4.7.2-%23478cbf?logo=godot-engine&logoColor=cyian&color=green">
</p>

<h1 align="center">Compatibility Overview</h1>
<table align="center">
  <thead>
    <tr>
      <th>api Version</th>
      <th>test.adapter Version</th>
      <th>analyzers Version</th>
      <th>.NET Version</th>
      <th>Minimum .NET SDK</th>
      <th>Godot minimal required/compatible Version</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>master (v5.1.0-rc6)</td><td>master (v3.2.0)</td><td>master (v1.1.0)</td><td>.NET 8, .NET 10</td><td>8.0.100</td>
      <td>v4.6, v4.6.3, v4.7.1, v4.7.2</td>
    </tr>
    <tr>
      <td>v5.1.0-rc1 - v5.1.0-rc5</td><td>v3.0.0 - v3.1.1</td><td>v1.0.0</td><td>.NET 8, .NET 9</td><td>8.0.100, analyzers 9.0.300</td><td>v4.3, v4.4, v4.4.1, v4.5</td>
    </tr>
    <tr>
      <td>v5.0.0</td><td>v3.0.0</td><td>v1.0.0</td><td>.NET 8, .NET 9</td><td>8.0.100, analyzers 9.0.300</td><td>v4.2, v4.2.1, v4.2.2, v4.3, v4.4</td>
    </tr>
    <tr>
      <td>v4.3.0, v4.3.1</td><td>v2.0.0</td><td>-</td><td>.NET 7, .NET 8</td><td>7.0.100</td><td>v4.2, v4.2.1, v4.2.2</td>
    </tr>
    <tr>
      <td>v4.2.4, v4.2.5</td><td>v1.1.2</td><td>-</td><td>.NET 7, .NET 8</td><td>7.0.100</td><td>v4.2, v4.2.1, v4.2.2</td>
    </tr>
    <tr>
      <td>v4.2.2, v4.2.3</td><td>v1.1.0, v1.1.1</td><td>-</td><td>.NET 7, .NET 8</td><td>7.0.100</td><td>v4.2, v4.2.1</td>
    </tr>
    <tr>
      <td>v4.2.1, v4.2.1.1</td><td>v1.0.0</td><td>-</td><td>.NET 7</td><td>7.0.100</td><td>v4.2, v4.2.1</td>
    </tr>
  </tbody>
</table>

<p align="center">
  The analyzers need the compiler of the named .NET SDK.
  The .NET SDK 9.0.300 ships with Visual Studio 17.14, the .NET SDK 8.0.100 with Visual Studio 17.8.<br/>
  With an older compiler, the build reports the warning <code>CS9057</code> and the analyzers do not run.
</p>

<p align="center">
  <img src="https://img.shields.io/badge/VSTest-Compatible-green?logo=visualstudio&logoColor=white" alt="VSTest compatible">
</p>

## What is gdUnit4Net

GdUnit4Net is a **feature-complete unit testing framework** for Godot C# projects that is **fully compatible with the VSTest standard**. It provides an API and VS test adapter to
run your Godot C# tests seamlessly in Visual Studio (Code), JetBrains Rider, and any VSTest-compatible environment.

### Key Highlights

🚀 **High Performance**: Up to 10x faster test execution for logic-only tests  
🎯 **VSTest Compatible**: Full integration with Visual Studio Test Platform  
⚡ **Smart Runtime**: Tests run without Godot runtime by default, with opt-in Godot features  
🔧 **Feature Complete**: All standard testing features you expect from modern frameworks

### Main Features

* **Writing, executing and debugging tests** with full IDE integration
* **Wide range of assertion methods** for verifying the behavior and output of your code
* **Parameterized Tests (Test Cases and DataPoints)** for testing functions with multiple sets of inputs and expected outputs
* **Advanced Test Filtering** with VSTest filter support, test categories, and traits
* **Scene runner** for simulating different kinds of inputs and actions, such as mouse clicks and keyboard inputs  
  For example, you can simulate mouse clicks and keyboard inputs by calling the appropriate methods on the runner instance. Additionally, you can wait for a specific signal to be
  emitted by the scene, or you can wait for a specific function to return a certain value.
* **VSTest Adapter** to run and debug your tests with full VSTest compatibility
* **Powerful test filtering capabilities** to selectively run tests based on various criteria
* **Exception monitoring** with automatic capture of Godot exceptions and runtime errors
* **Test output capture** including stdout and Godot log integration
* **Roslyn Analyzers** for compile-time validation of test attributes and combinations
* **Flexible test execution** - tests run without Godot runtime by default for maximum performance, with `[RequireGodotRuntime]` for Godot-specific features

### Architecture Redesign (v5.0+)

GdUnit4Net v5.0 introduces a **major architecture overhaul** that revolutionizes test performance and compatibility:

* **Smart Runtime Detection**: Tests run in lightweight mode by default, only spinning up Godot runtime when explicitly needed
* **Performance Boost**: Logic-only tests execute up to **10x faster** than previous versions
* **VSTest Standard Compliance**: Full compatibility with Visual Studio Test Platform and all VSTest features
* **Selective Godot Integration**: Use `[RequireGodotRuntime]` attribute only for tests that actually need Godot features

⚠️ Please read the [release notes](https://github.com/MikeSchulze/gdUnit4Net/releases/tag/v5.0.0) about breaking changes. ⚠️

There are three packages available in this project:

* **[gdUnit4.api](Api/README.md)** - The core package to enable writing and running unit tests in C#.
* **[gdUnit4.test.adapter](TestAdapter/README.md)** - The test adapter to integrate GdUnit4 with Visual Studio Test Platform.
* **[gdUnit4.analyzers](Analyzers/README.md)** - A Roslyn-based analyzer that provides compile-time validation for GdUnit4 test attributes.

## gdunit4.api

Checkout the [gdUnit4.api README](Api/README.md) to install and use the core testing framework.

## gdUnit4.test.adapter

Checkout the [gdUnit4.test.adapter README](TestAdapter/README.md) to install and use the test adapter. This adapter now supports
powerful [test filtering capabilities](TestAdapter/TestFilterGuide.md) for selectively running tests based on specific criteria.

## gdUnit4.analyzers

For compile-time validation of your test code, check out the [gdUnit4.analyzers README](Analyzers/README.md).

# Examples

The [gdUnit4NetExamples](https://github.com/MikeSchulze/gdUnit4NetExamples) repository provides comprehensive example projects
demonstrating both basic and advanced GdUnit4Net usage patterns. It includes foundational examples for newcomers learning unit testing fundamentals,
as well as sophisticated examples covering professional configurations, complex testing scenarios, input simulation, signal testing,
and exception handling. These working examples showcase GdUnit4Net v5.0 features including selective Godot runtime usage, advanced scene testing,
and production-ready test setups.

### Short Example

```csharp
namespace GdUnit4.Tests
{
    using static Assertions;

    [TestSuite]
    public class StringAssertTest
    {
        // Fast execution - no Godot runtime needed
        [TestCase]
        public void IsEqual()
        {
            AssertThat("This is a test message").IsEqual("This is a test message");
        }
        
        // Godot runtime required for Node operations
        [TestCase] 
        [RequireGodotRuntime]
        public void TestGodotNode()
        {
             AssertThat(new Node2D()).IsNotNull();
        }
        
        // Exception monitoring for Godot-specific errors
        [Test]
        [RequireGodotRuntime]
        [GodotExceptionMonitor]
        public void TestNodeCallback()
        {
            var node = new MyNode(); // Will catch exceptions in _Ready()
            AddChild(node);
        }
        
        // Data-driven tests with dynamic test data
        [Test]
        [DataPoint(nameof(TestData))]
        public void TestCalculations(int a, int b, int expected)
        {
            AssertThat(Calculator.Add(a, b)).IsEqual(expected);
        }
        
        // Exception validation with specific messages
        [Test]
        [ThrowsException(typeof(ArgumentNullException), "Value cannot be null")]
        public void TestValidation()
        {
            Calculator.Add(null, 5);
        }
        
        // Test categories and traits for filtering
        [Test]
        [Category("Integration")]
        [Trait("Speed", "Fast")]
        public void FastIntegrationTest()
        {
            // This test can be filtered by category or trait
        }
        
        // Data source for parameterized tests
        public static IEnumerable<object[]> TestData => new[]
        {
            new object[] { 1, 2, 3 },
            new object[] { 5, 7, 12 },
            new object[] { -1, 1, 0 }
        };
    }
}
```

### VSTest Integration Example

Run tests with advanced filtering using VSTest standard syntax:

```bash
# Run only fast tests
dotnet test --filter "Trait=Speed:Fast"

# Run integration tests
dotnet test --filter "Category=Integration"

# Run tests by name pattern
dotnet test --filter "FullyQualifiedName~Calculator"

# Combine multiple filters
dotnet test --filter "(Category=Integration)|(Trait=Speed:Fast)"
```

The test run looks like this:
<p align="center"><a href="https://github.com/MikeSchulze/gdUnit4Net"><img src="https://github.com/MikeSchulze/gdUnit4Net/blob/master/Example/assets/TestExplorerRun.png" width="100%"/></p><br/>

## What's New in v5.0

### 🚀 Performance Revolution

- **Up to 10x faster** test execution for logic-only tests
- Tests run without Godot runtime by default
- Only use `[RequireGodotRuntime]` when you actually need Godot features

### 🎯 VSTest Standard Compliance

- Full compatibility with Visual Studio Test Platform
- Advanced test filtering with categories and traits
- Seamless integration with CI/CD pipelines

### 📊 Enhanced Test Capabilities

- **DataPoint attributes** for dynamic parameterized tests
- **Exception monitoring** with automatic Godot error capture
- **Output capture** including stdout and Godot logs
- **Roslyn analyzers** for compile-time validation

### 🔧 Developer Experience

- Better error reporting and diagnostics
- Improved test discovery and execution
- Enhanced debugging capabilities
- Environment variable support from runsettings

## Documentation

<p align="left" style="font-family: Bedrock; font-size:21pt; color:#7253ed; font-style:bold">
  <a href="https://godot-gdunit-labs.github.io/gdUnit4/latest/csharp_project_setup/csharp-setup/">How to setup GdUnit4 for C#</a>
</p>

<p align="left" style="font-family: Bedrock; font-size:21pt; color:#7253ed; font-style:bold">
  <a href="https://godot-gdunit-labs.github.io/gdUnit4/latest/csharp_project_setup/vstest-adapter/">Supported IDE's</a>
</p>

<p align="left" style="font-family: Bedrock; font-size:21pt; color:#7253ed; font-style:bold">
  <a href="TestAdapter/TestFilterGuide.md">Test Filtering Guide</a>
</p>

### Migration from v4.x to v5.0

**Important**: v5.0 includes breaking changes that require minimal code updates:
⚠️ Please read the [release notes](https://github.com/MikeSchulze/gdUnit4Net/releases/tag/v5.0.0) about breaking changes. ⚠️

1. **Add `[RequireGodotRuntime]`** to tests that use Godot features (Nodes, Scenes, Resources)
2. **Remove unnecessary Godot dependencies** from pure logic tests for better performance
3. **Update test filtering** to use new VSTest-compatible syntax

### You Are Welcome To

* [Give Feedback](https://github.com/godot-gdunit-labs/gdUnit4Net/discussions)
* [Suggest Improvements](https://github.com/godot-gdunit-labs/gdUnit4Net/issues/new?assignees=MikeSchulze&labels=enhancement&template=feature_request.md&title=)
* [Report a Bug related to GdUnit4 API](https://github.com/godot-gdunit-labs/gdUnit4Net/issues/new?assignees=MikeSchulze&labels=bug%2Cgdunit4.api&projects=projects%2F6&template=bug_gdunit4_api.yaml&title=GD-XXX%3A+Describe+the+issue+briefly)
* [Report a Bug related to GdUnit4 Test Adapter](https://github.com/godot-gdunit-labs/gdUnit4Net/issues/new?assignees=MikeSchulze&labels=bug%2Cgdunit4.test.adapter&projects=projects%2F6&template=bug_gdunit4_test_adapter.yaml&title=GD-XXX%3A+Describe+the+issue+briefly)

---

### Contribution Guidelines

**Thank you for your interest in contributing to GdUnit4!**<br>
To ensure a smooth and collaborative contribution process, please review our [contribution guidelines](https://github.com/MikeSchulze/gdUnit4Net/blob/master/CONTRIBUTING.md) before
getting started. These guidelines outline the standards and expectations we uphold in this project.

Code of Conduct: We strictly adhere to the Godot code of conduct in this project. As a contributor, it is important to respect and follow this code to maintain a positive and
inclusive community.

Using GitHub Issues: We utilize GitHub issues for tracking feature requests and bug reports. If you have a general question or wish to engage in discussions, we recommend joining
the [GdUnit Discord Server](https://discord.gg/rdq36JwuaJ) for specific inquiries.

We value your input and appreciate your contributions to make GdUnit4 even better!

<p align="left">
  <a href="https://discord.gg/rdq36JwuaJ"><img src="https://discordapp.com/api/guilds/885149082119733269/widget.png?style=banner4" alt="Join GdUnit Server"/></a>
</p>

### Thank you for supporting my project

## Sponsors

[<img src="https://avatars.githubusercontent.com/u/4674635?v=4)" alt="Jeff" width="125"/>](https://github.com/jlb0170) Jeff

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
