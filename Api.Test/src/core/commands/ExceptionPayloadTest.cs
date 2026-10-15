namespace GdUnit4.Tests.Core.Commands;

using System;

using GdUnit4.Core.Commands;

using static Assertions;

[TestSuite]
public class ExceptionPayloadTest
{
    [TestCase]
    public void RoundTripDescribesOriginalException()
    {
        var exception = CatchThrownException();

        var current = ExceptionPayload.Deserialize(ExceptionPayload.Serialize(exception));

        AssertObject(current).IsNotNull();
        AssertString(current!.Message)
            .StartsWith("System.ArgumentException: invalid argument (Parameter 'value')\n")
            .Contains(nameof(CatchThrownException));
    }

    [TestCase]
    public void DeserializeEmptyPayload()
        => AssertObject(ExceptionPayload.Deserialize(string.Empty)).IsNull();

    [TestCase]
    public void DeserializePlainTextPayload()
    {
        var current = ExceptionPayload.Deserialize("Connection interrupted.");

        AssertObject(current).IsNotNull();
        AssertString(current!.Message).IsEqual("Connection interrupted.");
    }

    [TestCase]
    public void DeserializeIgnoresTypeDiscriminator()
    {
        var current = ExceptionPayload.Deserialize(/*lang=json,strict*/ """{"$type":"System.IO.FileInfo, System.IO.FileSystem","Type":"Foo.BarException","Message":"failed"}""");

        AssertObject(current).IsInstanceOf<InvalidOperationException>();
        AssertString(current!.Message).IsEqual("Foo.BarException: failed");
    }

    private static Exception CatchThrownException()
    {
        try
        {
            throw new ArgumentException("invalid argument", "value");
        }
        catch (ArgumentException e)
        {
            return e;
        }
    }
}
