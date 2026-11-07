// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Execution;

using System.Reflection;

using Extensions;

internal sealed class TestCase
{
    public TestCase(Guid id, MethodInfo methodInfo, int lineNumber, int attributeIndex)
    {
        Id = id;
        MethodInfo = methodInfo;
        Line = lineNumber;
        Parameters = InitialParameters();
        TestCaseAttribute = TestCaseAttributes[attributeIndex];
    }

    public string Name => MethodInfo.Name;

    public Guid Id { get; }

    public int Line { get; private set; }

    public List<TestCaseAttribute> TestCaseAttributes
        => [.. MethodInfo.GetCustomAttributes<TestCaseAttribute>().Where(TestParametersFilter)];

    public TestCaseAttribute TestCaseAttribute { get; init; }

    public MethodInfo MethodInfo { get; set; }

    public object?[] Arguments => IsParameterized ? TestCaseAttribute.Arguments : [.. Parameters.SelectMany(ResolveParam)];

    public bool IsSkipped => IgnoreUntilAttribute.IsSkipped(MethodInfo);

    public bool IsParameterized => TestCaseAttributes.Any(p => p.Arguments.Length > 0);

    internal bool HasDataPoint => DataPoint != null;

    internal DataPointAttribute? DataPoint => MethodInfo.GetCustomAttribute<DataPointAttribute>();

    private Func<TestCaseAttribute, bool> TestParametersFilter { get; } = _ => true;

    private IEnumerable<object> Parameters { get; }

    internal static string BuildDisplayName(string testName, TestCaseAttribute attribute, int attributeIndex = -1)
    {
        var name = attribute.TestName ?? testName;
        if (attributeIndex == -1)
            return name;

        var parameters = string
            .Join(", ", attribute.Arguments.Select(GdUnitExtensions.Formatted))
            .Replace(".", ",", StringComparison.Ordinal);
        return $"{name}:{attributeIndex} ({parameters})";
    }

    internal static string BuildFullyQualifiedName(string classNameSpace, string testName, TestCaseAttribute attr)
    {
        if (attr.Arguments.Length == 0)
            return $"{classNameSpace}.{attr.TestName ?? testName}";
        var parameterizedTestName = BuildDisplayName(testName, attr);
        return $"{classNameSpace}.{testName}.{parameterizedTestName}";
    }

    private IEnumerable<object> ResolveParam(object input)
    {
        if (input is IValueProvider provider)
            return provider.GetValues();
        return [input];
    }

    private List<object> InitialParameters()
        =>
        [
            .. MethodInfo.GetParameters()
                .SelectMany(pi => pi.GetCustomAttributesData()
                    .Where(attr => attr.AttributeType == typeof(FuzzerAttribute))
                    .Select(attr =>
                    {
                        var arguments = attr.ConstructorArguments.Select(arg => arg.Value).ToArray();
                        return attr.Constructor.Invoke(arguments);
                    }))
        ];
}
