namespace GdUnit4.TestAdapter.Test.Settings;

using System.IO;
using System.Xml;
using System.Xml.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TestAdapter.Settings;

[TestClass]
public class GdUnit4SettingsTest
{
    private static readonly XmlSerializer Serializer = new(typeof(GdUnit4Settings));

    [TestMethod]
    public void TolerateExitCodeAfterCompletionIsDisabledByDefault()
    {
        var settings = Deserialize("<GdUnit4></GdUnit4>");

        Assert.IsFalse(settings.TolerateExitCodeAfterCompletion);
    }

    [TestMethod]
    public void TolerateExitCodeAfterCompletionIsParsed()
    {
        var settings = Deserialize("<GdUnit4><TolerateExitCodeAfterCompletion>true</TolerateExitCodeAfterCompletion></GdUnit4>");

        Assert.IsTrue(settings.TolerateExitCodeAfterCompletion);
    }

    private static GdUnit4Settings Deserialize(string xml)
    {
        using var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
        return (GdUnit4Settings)Serializer.Deserialize(reader)!;
    }
}
