namespace GdUnit4.Tests.Extractors;

using GdUnit4.Extractors;
using static Assertions;

[TestSuite]
public class ValueExtractorTest
{
    private sealed class TestObject
    {
        public enum STATE
        {
            INIT,
            RUN
        }

        public TestObject()
        {
            State = STATE.INIT;
            TypeA = "aaa";
            TypeB = "bbb";
            TypeC = "ccc";
            Value = "none";
        }

        public STATE State { get; private set; }

        public TestObject? Parent { get; set; }

        public string Value { get; set; }

        public string TypeA { get; private set; }

#pragma warning disable CS0628 // New protected member declared in sealed type
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable IDE0052 // Remove unread private members
        protected string TypeB { get; private set; }

        private string TypeC { get; set; }

        public string GetA() => "getA";

        protected string GetB() => "getB";

        private string GetC() => "getC";
#pragma warning restore CS0628 // New protected member declared in sealed type
#pragma warning restore IDE0051 // Remove unused private members
#pragma warning restore IDE0052 // Remove unread private members
    }

    [TestCase]
    public void ExtractValueNotExists()
    {
        var obj = new TestObject();
        AssertString(new ValueExtractor("GetNNN").ExtractValue(obj) as string).IsEqual("n.a.");
    }

    [TestCase]
    public void ExtractValuePublicMethod()
    {
        var obj = new TestObject();
        AssertString(new ValueExtractor("GetA").ExtractValue(obj) as string).IsEqual("getA");
    }

    [TestCase]
    public void ExtractValueProtectedMethod()
    {
        var obj = new TestObject();

        AssertString(new ValueExtractor("GetB").ExtractValue(obj) as string).IsEqual("getB");
    }

    [TestCase]
    public void ExtractValuePrivateMethod()
    {
        var obj = new TestObject();

        AssertString(new ValueExtractor("GetC").ExtractValue(obj) as string).IsEqual("getC");
    }

    [TestCase]
    public void ExtractValuePublicProperty()
    {
        var obj = new TestObject();

        AssertString(new ValueExtractor("TypeA").ExtractValue(obj) as string).IsEqual("aaa");
    }

    [TestCase]
    public void ExtractValueProtectedProperty()
    {
        var obj = new TestObject();

        AssertString(new ValueExtractor("TypeB").ExtractValue(obj) as string).IsEqual("bbb");
    }

    [TestCase]
    public void ExtractValuePrivateProperty()
    {
        var obj = new TestObject();

        AssertString(new ValueExtractor("TypeC").ExtractValue(obj) as string).IsEqual("ccc");
    }

    [TestCase]
    public void ExtractValueEnum()
    {
        var obj = new TestObject();

        AssertObject(new ValueExtractor("State").ExtractValue(obj)).IsEqual(TestObject.STATE.INIT);
    }

    [TestCase]
    public void ExtractValueChained()
    {
        var obj = new TestObject();
        var parent = new TestObject
        {
            Value = "aaa"
        };
        obj.Parent = parent;

        AssertString(new ValueExtractor("Value").ExtractValue(obj) as string).IsEqual("none");
        AssertString(new ValueExtractor("Parent.Value").ExtractValue(obj) as string).IsEqual("aaa");
    }
}
