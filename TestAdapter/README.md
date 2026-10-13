# GdUnit4 Test Adapter

This is the GdUnit4 Test Adapter, designed to facilitate the integration of GdUnit4 with test frameworks supporting the Visual Studio Test Platform.

The GdUnit4.TestAdapter implements the Microsoft test adapter framework [VSTest](https://github.com/microsoft/vstest?tab=readme-ov-file#vstest).

## Features

- **Seamless Integration** with Visual Studio, VS Code, and JetBrains Rider
- **Test Discovery** to find all GdUnit4 tests in your project
- **Test Execution** to run tests directly from your IDE
- **Test Debugging** to step through your tests
- **Error Navigation** to jump directly to test failures
- **Solution Configuration** with test config files
- **Powerful Test Filtering** to selectively run tests based on various criteria

## Supported IDE's

| IDE                                | Test Discovery | Test Run | Test Debug | Jump to Failure | Solution test config file | Test Filter | Parallel Test Execution |
|------------------------------------|----------------|----------|------------|-----------------|---------------------------|-------------|-------------------------|
| Visual Studio                      | ✅              | ✅        | ✅          | ✅               | ✅                         | ✅           | ❌                       |
| Visual Studio Code                 | ✅              | ✅        | ✅          | ✅               | ✅                         | ✅           | ❌                       |
| JetBrains Rider min version 2024.2 | ✅              | ✅        | ✅          | ✅               | ✅                         | ✅           | ❌                       |

> ✅ - supported<br>
> ☑️ - supported by a workaround (link)<br>
> ❌ - not supported<br>

## Installation

Add the Test Adapter to your test project:

```xml
<PackageReference Include="gdUnit4.test.adapter" Version="3.0.0" />
```

## Test Filtering

GdUnit4Net now supports powerful test filtering capabilities, allowing you to selectively run tests based on various criteria such as test name, class, namespace, category, and
custom traits.

### Basic Filter Syntax

```
PropertyName=Value
```

### Example Filters

```
# Run only tests in a specific class
Class=CalculatorTests

# Run tests with a specific category
TestCategory=UnitTest

# Run tests with specific traits
Trait.Priority=High

# Combine filters with logical operators
TestCategory=UnitTest&Trait.Owner=TeamA
```

For detailed information about test filtering capabilities, including syntax, operators, examples, and best practices, see the [Test Filter Guide](TestFilterGuide.md).

## Configuration with .runsettings

GdUnit4Net Test Adapter supports extensive configuration through `.runsettings` files, allowing you to customize test execution behavior, environment variables, and
GdUnit4-specific settings.

### Basic .runsettings Setup

Create a `.runsettings` file in your solution root:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
    <RunConfiguration>
        <MaxCpuCount>1</MaxCpuCount>
        <TestAdaptersPaths>.</TestAdaptersPaths>
        <ResultsDirectory>./TestResults</ResultsDirectory>
        <TestSessionTimeout>1800000</TestSessionTimeout>
        <TreatNoTestsAsError>true</TreatNoTestsAsError>
        
        <!-- Environment variables available to tests -->
        <EnvironmentVariables>
            <GODOT_BIN>C:\Path\To\Godot_v4.4-stable_mono_win64.exe</GODOT_BIN>
        </EnvironmentVariables>
    </RunConfiguration>

    <!-- Test result loggers -->
    <LoggerRunSettings>
        <Loggers>
            <Logger friendlyName="console" enabled="True">
                <Configuration>
                    <Verbosity>normal</Verbosity>
                </Configuration>
            </Logger>
            <Logger friendlyName="html" enabled="True">
                <Configuration>
                    <LogFileName>test-result.html</LogFileName>
                </Configuration>
            </Logger>
            <Logger friendlyName="trx" enabled="True">
                <Configuration>
                    <LogFileName>test-result.trx</LogFileName>
                </Configuration>
            </Logger>
        </Loggers>
    </LoggerRunSettings>

    <!-- GdUnit4-specific configuration -->
    <GdUnit4>
        <!-- Additional Godot runtime parameters -->
        <Parameters>--verbose</Parameters>
        
        <!-- Test display name format: SimpleName or FullyQualifiedName -->
        <DisplayName>FullyQualifiedName</DisplayName>
        
        <!-- Capture stdout from test cases -->
        <CaptureStdOut>true</CaptureStdOut>
        
        <!-- Compilation timeout for large projects (milliseconds) -->
        <CompileProcessTimeout>20000</CompileProcessTimeout>
        
        <!-- Connect timeout to the Godot runtime host for slow or variable environments (milliseconds) -->
        <GodotConnectTimeout>10000</GodotConnectTimeout>
        
        <!-- Report a failing Godot exit after all test results as a warning instead of failing the run -->
        <TolerateExitCodeAfterCompletion>false</TolerateExitCodeAfterCompletion>
    </GdUnit4>
</RunSettings>
```

### GdUnit4 Configuration Options

| Setting                           | Description                                                     | Default      | Example                  |
|-----------------------------------|-----------------------------------------------------------------|--------------|--------------------------|
| `Parameters`                      | Additional command-line arguments passed to Godot               | `""`         | `"--verbose --headless"` |
| `DisplayName`                     | Test name format in results                                     | `SimpleName` | `FullyQualifiedName`     |
| `CaptureStdOut`                   | Capture test output in results                                  | `false`      | `true`                   |
| `CompileProcessTimeout`           | Godot compilation timeout (ms)                                  | `20000`      | `30000`                  |
| `GodotConnectTimeout`             | Godot runtime host connect timeout (ms)                         | `10000`      | `60000`                  |
| `TolerateExitCodeAfterCompletion` | Report a failing Godot exit after all test results as a warning | `false`      | `true`                   |

### Using .runsettings

**Visual Studio**:

- Test → Configure Run Settings → Select Solution Wide runsettings File
- Or place `.runsettings` in solution root (auto-detected)

**Command Line**:

```bash
dotnet test --settings .runsettings
```

**VS Code**: Configure in settings.json:

```json
{
    "dotnet-test-explorer.runSettingsPath": ".runsettings"
}
```

### Environment Variables

Environment variables defined in `.runsettings` are available to your tests:

```xml
<EnvironmentVariables>
    <GODOT_BIN>C:\Godot\Godot.exe</GODOT_BIN>
    <TEST_DATA_PATH>./TestData</TEST_DATA_PATH>
    <DEBUG_MODE>true</DEBUG_MODE>
</EnvironmentVariables>
```

Access in tests:

```csharp
[Test]
public void TestWithEnvironmentVariable()
{
    var godotPath = Environment.GetEnvironmentVariable("GODOT_BIN");
    var debugMode = Environment.GetEnvironmentVariable("DEBUG_MODE");
    // Use environment variables in your tests
}
```

### Advanced Logging Configuration

Configure multiple output formats for comprehensive test reporting:

```xml
<LoggerRunSettings>
    <Loggers>
        <!-- Console output -->
        <Logger friendlyName="console" enabled="True">
            <Configuration>
                <Verbosity>detailed</Verbosity>
            </Configuration>
        </Logger>
        
        <!-- HTML report -->
        <Logger friendlyName="html" enabled="True">
            <Configuration>
                <LogFileName>TestResults/test-report.html</LogFileName>
            </Configuration>
        </Logger>
        
        <!-- TRX format for CI/CD -->
        <Logger friendlyName="trx" enabled="True">
            <Configuration>
                <LogFileName>TestResults/test-results.trx</LogFileName>
            </Configuration>
        </Logger>
    </Loggers>
</LoggerRunSettings>
```

## Related Packages

* [gdUnit4.api](../Api/README.md) - The core testing framework
* [gdUnit4.analyzers](../Analyzers/README.md) - Add compile-time validation for your test code

## Documentation

The full documentation can be found [here](https://godot-gdunit-labs.github.io/gdUnit4/latest/csharp_project_setup/vstest-adapter/).

### You Are Welcome To

* [Give Feedback](https://github.com/MikeSchulze/gdUnit4Net/discussions)
* [Suggest Improvements](https://github.com/MikeSchulze/gdUnit4Net/issues/new?assignees=MikeSchulze&labels=enhancement&template=feature_request.md&title=)
* [Report a Bug related to GdUnit4 API](https://github.com/MikeSchulze/gdUnit4Net/issues/new?assignees=MikeSchulze&labels=bug%2Cgdunit4.api&projects=projects%2F6&template=bug_gdunit4_api.yaml&title=GD-XXX%3A+Describe+the+issue+briefly)
* [Report a Bug related to GdUnit4 Test Adapter](https://github.com/MikeSchulze/gdUnit4Net/issues/new?assignees=MikeSchulze&labels=bug%2Cgdunit4.test.adapter&projects=projects%2F6&template=bug_gdunit4_test_adapter.yaml&title=GD-XXX%3A+Describe+the+issue+briefly)

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

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.

---
