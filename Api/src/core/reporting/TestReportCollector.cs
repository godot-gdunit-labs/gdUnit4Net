// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core.Reporting;

using System.Collections.Generic;
using System.Linq;

using Api;

internal sealed class TestReportCollector
{
    public List<ITestReport> Reports { get; } = [];

    public IEnumerable<ITestReport> Failures => Reports.Where(r => r.IsFailure);

    public IEnumerable<ITestReport> Errors => Reports.Where(r => r.IsError);

    public IEnumerable<ITestReport> Warnings => Reports.Where(r => r.IsWarning);

    public void Consume(ITestReport report) => Reports.Add(report);

    public void PushFront(ITestReport report) => Reports.Insert(0, report);

    public void Clear() => Reports.Clear();
}
