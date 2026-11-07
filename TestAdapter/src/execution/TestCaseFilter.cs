// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter.Execution;

using Api;

using Extensions;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

/// <summary>
///     Implementation for filtering test cases.
/// </summary>
internal class TestCaseFilter
{
    private readonly ITestCaseFilterExpression? filterExpression;

    public TestCaseFilter(IRunContext runContext, ITestEngineLogger logger)
    {
        try
        {
            filterExpression = runContext.GetTestCaseFilter(
                TestCaseExtensions.SupportedProperties.Keys,
                TestCaseExtensions.GetPropertyProvider());
        }
        catch (TestPlatformFormatException e)
        {
            logger.LogError(e.Message);
        }
    }

    /// <summary>
    ///     Runs the filter on the provided test cases and returns the filtered collection.
    /// </summary>
    /// <param name="testCases">The collection of test cases to filter.</param>
    /// <returns>The filtered collection of test cases or the original collection if the filter is null.</returns>
    public List<TestCase> Execute(List<TestCase> testCases) =>
        filterExpression == null
            ? testCases
            : [.. testCases.Where(MatchTestCase)];

    /// <summary>
    ///     Determines if a test case matches the filter criteria.
    /// </summary>
    /// <param name="testCase">Test case to evaluate.</param>
    /// <returns>True if the test case matches the filter, false otherwise.</returns>
    private bool MatchTestCase(TestCase testCase)
        => filterExpression?.MatchTestCase(testCase, testCase.GetPropertyValue) ?? false;
}
