# The C# GdUnit4 Analyzers

## What is GdUnit4.Analyzer

GdUnit4.Analyzer is a Roslyn-based analyzer package designed to enhance the development experience when writing tests with GdUnit4. It provides compile-time
validation for GdUnit4 test attributes and helps developers catch configuration errors early in the development process.

## Features

The analyzer must be included by referencing the gdUnit4.analyzer package:

```xml
<PackageReference Include="gdUnit4.api" Version="5.0.0"/>
<PackageReference Include="gdUnit4.test.adapter" Version="3.0.0"/>
<PackageReference Include="gdUnit4.analyzers" Version="1.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

* **Attribute Validation:** The analyzer enforces correct usage of GdUnit4 test attributes

  #### Example: DataPoint and TestCase Combination Validation

  Validates proper combination of DataPoint and TestCase attributes:

  ```csharp
  // ✅ Valid: Single TestCase with DataPoint
  [TestCase]
  [DataPoint(nameof(TestData))]
  public void ValidTest(int a, int b) { }
  
  // ❌ Invalid: Multiple TestCase with DataPoint
  [TestCase]
  [TestCase]                         // GdUnit0201 error: Method 'InvalidTest' cannot have multiple TestCase attributes when DataPoint attribute is present
  [DataPoint(nameof(TestData))]
  public void InvalidTest(int a, int b) { }
  ```

## Related Packages

* [gdUnit4.api](../Api/README.md) - The core testing framework
* [gdUnit4.test.adapter](../TestAdapter/README.md) - Run your tests in Visual Studio, VS Code, and JetBrains Rider with filtering support

## Technical Details

The analyzer is built using:

* .NET Standard 2.0
* Roslyn Analyzer Framework
* Microsoft.CodeAnalysis.CSharp

## Documentation

For more detailed documentation about the entire GdUnit4 ecosystem, visit our [documentation site](https://mikeschulze.github.io/gdUnit4/).

### You are welcome to

* [Give Feedback](https://github.com/MikeSchulze/gdUnit4Net/discussions)
* [Suggest Improvements](https://github.com/MikeSchulze/gdUnit4Net/issues/new?assignees=MikeSchulze&labels=enhancement&template=feature_request.md&title=)
* [Report Bugs](https://github.com/MikeSchulze/gdUnit4Net/issues/new?assignees=MikeSchulze&labels=bug%2C+task&template=bug_report.md&title=)

## License

This project is licensed under the MIT License - see the [LICENSE](../LICENSE) file for details.
