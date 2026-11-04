# GdUnit4 Test Filter Usage Guide

## Introduction

GdUnit4 supports VSTest filtering capabilities, allowing you to selectively run tests based on various criteria such as
test name, class, namespace, category, and custom traits. This functionality integrates with the standard VSTest
platform's filtering syntax and can be used from the command line, Visual Studio, Visual Studio Code, or Rider.

## Supported Filter Properties

The following properties can be used for filtering:

| Property               | Description                                 | Example                                             |
|------------------------|---------------------------------------------|-----------------------------------------------------|
| **FullyQualifiedName** | The fully qualified name of the test method | `FullyQualifiedName=MyNamespace.MyClass.TestMethod` |
| **Name**               | The display name of the test                | `Name=TestAddition`                                 |
| **Class**              | The class name containing the test          | `Class=CalculatorTests`                             |
| **Namespace**          | The namespace of the test class             | `Namespace=MyProject.Tests`                         |
| **TestCategory**       | The category assigned to the test           | `TestCategory=UnitTest`                             |
| **Trait.{name}**       | Custom traits                               | `Trait.Owner=TeamA`                                 |

## Filter Syntax

The basic filter syntax follows this pattern:

```text
PropertyName=Value
```

### Comparison Operators

The following comparison operators are supported:

| Operator | Description      | Example                         |
|----------|------------------|---------------------------------|
| `=`      | Equal to         | `TestCategory=UnitTest`         |
| `!=`     | Not equal to     | `TestCategory!=IntegrationTest` |
| `~`      | Contains         | `FullyQualifiedName~Calculator` |
| `!~`     | Does not contain | `Name!~Legacy`                  |

### Logical Operators

You can combine multiple conditions using logical operators:

| Operator | Description | Example                                    |
|----------|-------------|--------------------------------------------|
| `&`      | AND         | `TestCategory=UnitTest&Class=Calculator`   |
| `\|`     | OR          | `TestCategory=UnitTest\|TestCategory=Fast` |
| `!`      | NOT         | `!TestCategory=SlowTest`                   |

You can also use parentheses to group expressions:

```text
(TestCategory=UnitTest|TestCategory=Fast)&Namespace=MyProject.Tests
```

## Categorizing Tests

GdUnit4 provides two ways to categorize your tests: using the `TestCategory` attribute or using the more flexible
`Trait` attribute.

### Using TestCategory

The `TestCategory` attribute is the simplest way to categorize tests:

```csharp
using GdUnit4.Attributes;

[TestSuite]
public class CalculatorTests
{
    [TestCase]
    [TestCategory("UnitTest")]
    [TestCategory("Math")]
    public void TestAddition()
    {
        // Test code here
    }
}
```

You can also apply categories at the class level to categorize all tests in a class:

```csharp
using GdUnit4.Attributes;

[TestSuite]
[TestCategory("Integration")]
public class DatabaseTests 
{
    [TestCase]
    public void TestConnection() 
    {
        // All tests in this class will have the "Integration" category
    }
}
```

### Using Traits

The `Trait` attribute provides a more flexible key-value approach:

```csharp
using GdUnit4.Attributes;

[TestSuite]
public class UserServiceTests
{
    [TestCase]
    [Trait("Category", "Integration")]
    [Trait("Owner", "TeamA")]
    [Trait("Priority", "High")]
    public void TestUserRegistration()
    {
        // Test code here
    }
}
```

Like `TestCategory`, traits can also be applied at the class level:

```csharp
using GdUnit4.Attributes;

[TestSuite]
[Trait("Category", "Performance")]
[Trait("Environment", "Production")]
public class BenchmarkTests 
{
    // All tests in this class will have these traits
}
```

## Using Test Filters

### From Command Line

When using the `dotnet test` command, you can apply filters with the `--filter` option:

```bash
dotnet test --filter "TestCategory=UnitTest"
```

Multiple filters can be applied:

```bash
dotnet test --filter "(TestCategory=UnitTest|TestCategory=Fast)&Namespace=MyProject.Tests"
```

### From Visual Studio

1. Go to Test Explorer
2. Click on the filter icon (or press Ctrl+E, T)
3. Enter your filter expression in the search box
4. Press Enter to apply the filter

### From Visual Studio Code

If you're using the .NET Test Explorer extension:

1. Open the Test Explorer view
2. Click on the filter icon
3. Enter your filter expression
4. Press Enter to apply the filter

### From Rider

1. Go to the Unit Tests window
2. Click on the filter icon
3. Enter your filter expression
4. Press Enter to apply the filter

## Filter Examples

Here are some practical examples:

### Running tests by category

```text
TestCategory=UnitTest
```

### Running tests from a specific namespace

```text
Namespace=GdUnit4.Tests.Core
```

### Running tests from a specific class that aren't integration tests

```text
Class=CalculatorTests&TestCategory!=Integration
```

### Running tests that match specific naming patterns

```text
FullyQualifiedName~Test.*Add.*
```

### Running tests with specific traits

```text
Trait.Owner=TeamA
```

### Running tests with complex conditions

```text
(TestCategory=UnitTest|TestCategory=Fast)&Class=CalculatorTests
```

### Running tests that don't contain a specific string in their name

```text
Name!~Legacy
```

### Running only high priority tests from a specific component

```text
Trait.Priority=High&Trait.Component=Core
```

## Implementation Details

### How Categories and Traits Work Together

When you use both `TestCategory` attributes and `Trait` attributes with a name of "Category", they're combined. This
means that all of the following are treated as categories:

```csharp
[TestCategory("UnitTest")]
[Trait("Category", "Fast")]
public void TestMethod()
{
    // This test has both "UnitTest" and "Fast" categories
}
```

### Inheritance of Categories and Traits

Categories and traits defined at the class level are inherited by all test methods in the class. If a test method
defines its own categories or traits, they are combined with those from the class.

```csharp
[TestSuite]
[TestCategory("Integration")]
public class DatabaseTests 
{
    [TestCase]
    // Inherits "Integration" category from the class
    public void TestConnection() { }
    
    [TestCase]
    [TestCategory("Slow")]
    // Has both "Integration" and "Slow" categories
    public void TestBulkInsert() { }
}
```

### VSTest Platform Integration

GdUnit4's filter implementation is built on top of the VSTest platform's filtering capabilities, ensuring compatibility
with all VSTest tools:

- Visual Studio Test Explorer
- VSCode .NET Test Explorer
- JetBrains Rider
- Command line (`dotnet test`)

This means the syntax and behavior match what you'd expect from other .NET testing frameworks.

## Best Practices

1. **Use Categories Consistently**
    - Establish a consistent set of categories across your test suite
    - Document your category system for team reference
    - Consider standard categories like "UnitTest", "IntegrationTest", "Fast", "Slow"

2. **Leverage Class-Level Categories**
    - Apply categories at the class level when all tests in a class share a category
    - This reduces repetition and makes your code more maintainable

3. **Combine with Test Runner Configuration**
    - Use filters in combination with test runner configuration for CI/CD pipelines
    - For example, run only fast unit tests during regular builds, and all tests in nightly builds

4. **Use Traits for Rich Metadata**
    - Use traits for additional metadata beyond simple categorization
    - Good candidates for traits include "Owner", "Priority", "Feature", "Component"

5. **Limit Trait Proliferation**
    - Avoid creating too many different trait names
    - Standardize trait names and values across your team

## Common Filter Patterns for CI/CD

Here are some filter patterns that are useful in CI/CD pipelines:

### Fast Tests for PR Builds

```bash
dotnet test --filter "TestCategory=UnitTest|TestCategory=Fast"
```

### Integration Tests for Nightly Builds

```bash
dotnet test --filter "TestCategory=IntegrationTest"
```

### Component-Specific Tests for Feature Branches

```bash
dotnet test --filter "Trait.Component=Authentication"
```

### Critical Tests for Quick Feedback

```bash
dotnet test --filter "Trait.Priority=High"
```

## Troubleshooting

If your filters aren't working as expected, try these troubleshooting steps:

1. **Check Filter Syntax**: Ensure your filter expression is correctly formatted
2. **Verify Test Attributes**: Check that the tests have the correct attributes applied
3. **Use Simple Filters First**: Start with simple filters and gradually add complexity
4. **Escape Special Characters**: Some shells require escaping of characters like `|` and `&`
5. **Quotes in Command Line**: When using filters from the command line, wrap the filter expression in quotes

## Conclusion

Test filtering provides a powerful way to manage large test suites and focus on relevant tests. With GdUnit4's support
for both `TestCategory` and `Trait` attributes, you have flexible options for organizing and selectively running your
tests.
