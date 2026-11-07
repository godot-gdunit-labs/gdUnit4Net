// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter.Discovery;

using System.Collections.Concurrent;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

internal sealed class TestCaseDiscoverySink : ITestCaseDiscoverySink
{
    private readonly ConcurrentBag<TestCase> testCases = [];

    public IReadOnlyList<TestCase> TestCases => [.. testCases.OrderBy(tc => tc.FullyQualifiedName)];

    public void SendTestCase(TestCase test) => testCases.Add(test);
}
