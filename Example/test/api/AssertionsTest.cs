namespace Examples.Test.Api;

using System.Collections;

using GdUnit4;

using static GdUnit4.Assertions;

using Vector2 = System.Numerics.Vector2;

[TestSuite]
public class AssertionsTest
{
    [TestCase]
    public void DoAssertNotYetImplemented()
        => AssertThrown(AssertNotYetImplemented)
            .HasFileLineNumber(16)
            .HasMessage("Test not yet implemented!");


    [TestCase(Description = "https://github.com/MikeSchulze/gdUnit4Net/issues/84")]
    public void AssertThatOnDynamics()
    {
        // object asserts
        AssertThat(Vector2.One).IsEqual(Vector2.One);
        AssertThat(TimeSpan.FromMilliseconds(124)).IsEqual(TimeSpan.FromMilliseconds(124));

        // dictionary asserts
        AssertThat(new Hashtable()).HasSize(0);

        // enumerable asserts
        AssertThat(new List<int>()).HasSize(0);
    }
}
