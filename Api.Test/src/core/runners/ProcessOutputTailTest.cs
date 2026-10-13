namespace GdUnit4.Tests.Core.Runners;

using GdUnit4.Core.Runners;

using static Assertions;

[TestSuite]
public class ProcessOutputTailTest
{
    [TestCase]
    public void KeepsLinesInOrder()
    {
        var tail = new ProcessOutputTail(3);
        tail.Add("line 1");
        tail.Add("line 2");

        AssertThat(tail.ToArray()).ContainsExactly("line 1", "line 2");
    }

    [TestCase]
    public void DropsOldestLinesAtCapacity()
    {
        var tail = new ProcessOutputTail(3);
        tail.Add("line 1");
        tail.Add("line 2");
        tail.Add("line 3");
        tail.Add("line 4");

        AssertThat(tail.ToArray()).ContainsExactly("line 2", "line 3", "line 4");
    }

    [TestCase]
    public void IgnoresBlankLinesAndTrimsTrailingWhitespace()
    {
        var tail = new ProcessOutputTail(3);
        tail.Add(null);
        tail.Add(string.Empty);
        tail.Add("   ");
        tail.Add("  indented backtrace frame  \r");

        // leading indentation of a backtrace is kept
        AssertThat(tail.ToArray()).ContainsExactly("  indented backtrace frame");
    }

    [TestCase]
    public void Clear()
    {
        var tail = new ProcessOutputTail(3);
        tail.Add("line 1");

        tail.Clear();

        AssertThat(tail.ToArray()).IsEmpty();
    }
}
