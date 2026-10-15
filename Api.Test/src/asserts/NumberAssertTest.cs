namespace GdUnit4.Tests.Asserts;

using GdUnit4.Core.Execution;
using GdUnit4.Core.Execution.Exceptions;

using static Assertions;

[TestSuite]
public class NumberAssertTest
{
    [TestCase]
    public void IsNull()
        => AssertThrown(() => AssertThat(23).IsNull())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(13)
            .HasMessage("""
                        Expecting be <Null>:
                         but is
                            '23'
                        """);

    [TestCase]
    public void IsNotNull()
    {
        AssertThat(-23).IsNotNull();
        AssertThat(0).IsNotNull();
        AssertThat(23).IsNotNull();
    }

    [TestCase]
    public void IsEqual()
    {
        AssertThat(-23).IsEqual(-23);
        AssertThat(0).IsEqual(0);
        AssertThat(23).IsEqual(23);

        // this assertion fails because 23 are not equal to 42
        AssertThrown(() => AssertThat(38).IsEqual(42))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(38)
            .HasMessage("""
                        Expecting be equal:
                            '42' but is '38'
                        """);
    }

    [TestCase]
    public void IsNotEqual()
    {
        AssertThat(23).IsNotEqual(-23);
        AssertThat(23).IsNotEqual(42);

        // this assertion fails because 23 are equal to 23
        AssertThrown(() => AssertThat(23).IsNotEqual(23))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(54)
            .HasMessage("""
                        Expecting be NOT equal:
                            '23' but is '23'
                        """);
    }

    [TestCase]
    public void IsLess()
    {
        AssertThat(-23).IsLess(-22);
        AssertThat(23).IsLess(42);
        AssertThat(23).IsLess(24);

        // this assertion fails because 23 is not less than 23
        AssertThrown(() => AssertThat(23).IsLess(23))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(71)
            .HasMessage("""
                        Expecting to be less than:
                            '23' but is '23'
                        """);
        AssertThrown(() => AssertThat(23).IsLess(22))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting to be less than:
                            '22' but is '23'
                        """);
        AssertThrown(() => AssertThat(-23).IsLess(-23))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting to be less than:
                            '-23' but is '-23'
                        """);
        AssertThrown(() => AssertThat(-23).IsLess(-24))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting to be less than:
                            '-24' but is '-23'
                        """);
    }

    [TestCase]
    public void IsLessEqual()
    {
        AssertThat(-23).IsLessEqual(-22);
        AssertThat(-23).IsLessEqual(-23);
        AssertThat(0).IsLessEqual(0);
        AssertThat(23).IsLessEqual(23);
        AssertThat(23).IsLessEqual(42);

        // this assertion fails because 23 is not less than or equal to 22
        AssertThrown(() => AssertThat(23).IsLessEqual(22)).IsInstanceOf<TestFailedException>()
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(108)
            .HasMessage("""
                        Expecting to be less than or equal:
                            '22' but is '23'
                        """);
        AssertThrown(() => AssertThat(-23).IsLessEqual(-24))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting to be less than or equal:
                            '-24' but is '-23'
                        """);
    }

    [TestCase]
    public void IsGreater()
    {
        AssertThat(-23).IsGreater(-24);
        AssertThat(1).IsGreater(0);
        AssertThat(23).IsGreater(20);
        AssertThat(23).IsGreater(22);

        // this assertion fails because 23 is not greater than 23
        AssertThrown(() => AssertThat(23).IsGreater(23))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(132)
            .HasMessage("""
                        Expecting to be greater than:
                            '23' but is '23'
                        """);
        AssertThrown(() => AssertThat(23).IsGreater(24))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting to be greater than:
                            '24' but is '23'
                        """);
        AssertThrown(() => AssertThat(-23).IsGreater(-23))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting to be greater than:
                            '-23' but is '-23'
                        """);
        AssertThrown(() => AssertThat(-23).IsGreater(-22))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting to be greater than:
                            '-22' but is '-23'
                        """);
    }

    [TestCase]
    public void IsGreaterEqual()
    {
        AssertThat(-23).IsGreaterEqual(-24);
        AssertThat(-23).IsGreaterEqual(-23);
        AssertThat(0).IsGreaterEqual(0);
        AssertThat(23).IsGreaterEqual(20);
        AssertThat(23).IsGreaterEqual(23);

        // this assertion fails because 23 is not greater than 23
        AssertThrown(() => AssertThat(23).IsGreaterEqual(24))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(169)
            .HasMessage("""
                        Expecting to be greater than or equal:
                            '24' but is '23'
                        """);
        AssertThrown(() => AssertThat(-23).IsGreaterEqual(-22))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting to be greater than or equal:
                            '-22' but is '-23'
                        """);
    }

    [TestCase]
    public void IsEqualApprox()
    {
        AssertThat(42).IsEqualApprox(42, 0);
        AssertThat(42).IsEqualApprox(40, 2);
        AssertThat(42.333).IsEqualApprox(42.333, 0);
        AssertThat(42.333).IsEqualApprox(40, 2.333);

        AssertThrown(() => AssertThat(42).IsEqualApprox(40, 1))
            .HasFileLineNumber(192)
            .HasMessage("""
                        Expecting:
                            '42'
                         in range between
                            '39' <> '41'
                        """);
        AssertThrown(() => AssertThat(42.333).IsEqualApprox(40, 2.133))
            .HasMessage("""
                        Expecting:
                            '42.333'
                         in range between
                            '37.867' <> '42.133'
                        """);
    }

    [TestCase]
    public void IsEven()
    {
        AssertThat(-200).IsEven();
        AssertThat(-22).IsEven();
        AssertThat(0).IsEven();
        AssertThat(22).IsEven();
        AssertThat(200).IsEven();

        AssertThrown(() => AssertThat(-13).IsEven())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(218)
            .HasMessage("""
                        Expecting be even:
                         but is '-13'
                        """);
        AssertThrown(() => AssertThat(13).IsEven())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be even:
                         but is '13'
                        """);
    }

    [TestCase]
    public void IsOdd()
    {
        AssertThat(-13).IsOdd();
        AssertThat(13).IsOdd();
        AssertThrown(() => AssertThat(-12).IsOdd())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(238)
            .HasMessage("""
                        Expecting be odd:
                         but is '-12'
                        """);
        AssertThrown(() => AssertThat(0).IsOdd())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be odd:
                         but is '0'
                        """);
        AssertThrown(() => AssertThat(12).IsOdd())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be odd:
                         but is '12'
                        """);
    }

    [TestCase]
    public void IsNegative()
    {
        AssertThat(-1).IsNegative();
        AssertThat(-23).IsNegative();
        AssertThrown(() => AssertThat(0).IsNegative())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(264)
            .HasMessage("""
                        Expecting be negative:
                         but is '0'
                        """);
        AssertThrown(() => AssertThat(13).IsNegative())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be negative:
                         but is '13'
                        """);
    }

    [TestCase]
    public void IsNotNegative()
    {
        AssertThat(0).IsNotNegative();
        AssertThat(13).IsNotNegative();
        AssertThrown(() => AssertThat(-1).IsNotNegative())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(284)
            .HasMessage("""
                        Expecting be NOT negative:
                         but is '-1'
                        """);
        AssertThrown(() => AssertThat(-13).IsNotNegative())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be NOT negative:
                         but is '-13'
                        """);
    }

    [TestCase]
    public void IsZero()
    {
        AssertThat(0).IsZero();

        // this assertion fail because the value is not zero
        AssertThrown(() => AssertThat(-1).IsZero())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(305)
            .HasMessage("""
                        Expecting be zero:
                         but is '-1'
                        """);
        AssertThrown(() => AssertThat(1).IsZero())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting be zero:
                         but is '1'
                        """);
    }

    [TestCase]
    public void IsNotZero()
    {
        AssertThat(-1).IsNotZero();
        AssertThat(1).IsNotZero();

        // this assertion fail because the value is not zero
        AssertThrown(() => AssertThat(0).IsNotZero())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(327)
            .HasMessage("""
                        Expecting be NOT zero:
                         but is '0'
                        """);
    }

    [TestCase]
    public void IsIn()
    {
        AssertThat(5).IsIn(3, 4, 5, 6);
        AssertThat(5).IsIn(3, 4, 5, 6);

        // this assertion fail because 7 is not in [3, 4, 5, 6]
        AssertThrown(() => AssertThat(7).IsIn(3, 4, 5, 6))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(343)
            .HasMessage("""
                        Expecting:
                            '7'
                         is in
                            [3, 4, 5, 6]
                        """);
        AssertThrown(() => AssertThat(7).IsIn())
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting:
                            '7'
                         is in
                            <Empty>
                        """);
    }

    [TestCase]
    public void IsNotIn()
    {
        AssertThat(5).IsNotIn();
        AssertThat(5).IsNotIn();
        AssertThat(5).IsNotIn(3, 4, 6, 7);
        AssertThat(5).IsNotIn(3, 4, 6, 7);

        // this assertion fail because 7 is not in [3, 4, 5, 6]
        AssertThrown(() => AssertThat(5).IsNotIn(3, 4, 5, 6))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(371)
            .HasMessage("""
                        Expecting:
                            '5'
                         is not in
                            [3, 4, 5, 6]
                        """);
    }

    [TestCase(Iterations = 40)]
    public void IsBetween([Fuzzer(-20)] int value)
        => AssertThat(value).IsBetween(-20, 20);

    [TestCase]
    public void IsBetweenMustFail()
    {
        AssertThrown(() => AssertThat(-10).IsBetween(-9, 0))
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(389)
            .HasMessage("""
                        Expecting:
                            '-10'
                         in range between
                            '-9' <> '0'
                        """);
        AssertThrown(() => AssertThat(0).IsBetween(1, 10))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting:
                            '0'
                         in range between
                            '1' <> '10'
                        """);
        AssertThrown(() => AssertThat(10).IsBetween(11, 21))
            .IsInstanceOf<TestFailedException>()
            .HasMessage("""
                        Expecting:
                            '10'
                         in range between
                            '11' <> '21'
                        """);
    }

    [TestCase]
    public void OverrideFailureMessage()
        => AssertThrown(() => AssertThat(10)
                .OverrideFailureMessage("Custom failure message")
                .IsNull())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(418)
            .HasMessage("Custom failure message");

    [TestCase]
    public void AppendFailureMessage()
        => AssertThrown(() => AssertThat(10)
                .AppendFailureMessage("custom data")
                .IsNull())
            .IsInstanceOf<TestFailedException>()
            .HasFileLineNumber(427)
            .HasMessage("""
                        Expecting be <Null>:
                         but is
                            '10'

                        Additional info:
                        custom data
                        """);

    [TestCase]
    public void InterruptIsFailure()
    {
        // we disable failure reporting until we simulate an failure
        ExecutionContext.Current?.FailureReporting = false;

        // force an assertion failure
        AssertThat(10).IsZero();

        // expect this line will never called because of the test is interrupted by a failing assert
        AssertBool(true).OverrideFailureMessage("This line should never be called").IsFalse();
    }

    [TestCase]
    public void MethodChainingBaseAssert()
    {
        AssertThat(42).IsNotNull().IsGreater(10).IsLess(50);
        AssertThat(42).IsGreater(10).IsNotNull().IsLess(50);
    }
}
