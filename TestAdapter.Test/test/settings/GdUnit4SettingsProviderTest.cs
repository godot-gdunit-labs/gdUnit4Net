namespace GdUnit4.TestAdapter.Test.Settings;

using System;
using System.IO;
using System.Xml;

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

using TestAdapter.Settings;

[TestClass]
public class GdUnit4SettingsProviderTest
{
    [TestMethod]
    public void LoadParsesSettings()
    {
        var settings = Load("<GdUnit4><TolerateExitCodeAfterCompletion>true</TolerateExitCodeAfterCompletion></GdUnit4>");

        Assert.IsTrue(settings.TolerateExitCodeAfterCompletion);
    }

    [TestMethod]
    public void LoadWithInvalidValueUsesDefaults()
    {
        // the serializer reports a value that does not match the settings type as InvalidOperationException
        var settings = Load("<GdUnit4><TolerateExitCodeAfterCompletion>maybe</TolerateExitCodeAfterCompletion></GdUnit4>");

        Assert.IsFalse(settings.TolerateExitCodeAfterCompletion);
    }

    [TestMethod]
    public void LoadWithMalformedXmlUsesDefaults()
    {
        // the reader reports the malformed start element as XmlException
        var settings = Load("<GdUnit4 TolerateExitCodeAfterCompletion=>");

        Assert.IsFalse(settings.TolerateExitCodeAfterCompletion);
    }

    [TestMethod]
    public void LoadPropagatesUnexpectedException()
    {
        var reader = new Mock<XmlReader>();
        _ = reader.Setup(r => r.Read()).Throws(new NotSupportedException("unexpected"));

        _ = Assert.ThrowsExactly<NotSupportedException>(() => new GdUnit4SettingsProvider().Load(reader.Object));
    }

    private static GdUnit4Settings Load(string xml)
    {
        var provider = new GdUnit4SettingsProvider();
        using (var reader = XmlReader.Create(new StringReader(xml), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
            provider.Load(reader);

        var discoveryContext = new Mock<IDiscoveryContext>();
        _ = discoveryContext
            .Setup(context => context.RunSettings!.GetSettings(GdUnit4Settings.RUN_SETTINGS_XML_NODE))
            .Returns(provider);
        return GdUnit4SettingsProvider.LoadSettings(discoveryContext.Object);
    }
}
