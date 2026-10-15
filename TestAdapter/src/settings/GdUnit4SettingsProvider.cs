// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter.Settings;

using System.Xml;
using System.Xml.Serialization;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

[SettingsName(GdUnit4Settings.RUN_SETTINGS_XML_NODE)]

// ReSharper disable once ClassNeverInstantiated.Global
internal sealed class GdUnit4SettingsProvider : ISettingsProvider
{
    private GdUnit4Settings Settings { get; set; } = new();

    private XmlSerializer Serializer { get; } = new(typeof(GdUnit4Settings));

    public void Load(XmlReader reader)
    {
        try
        {
            if (reader.Read() && reader.Name == GdUnit4Settings.RUN_SETTINGS_XML_NODE)
            {
                var settings = Serializer.Deserialize(reader) as GdUnit4Settings;
                Settings = settings ?? new GdUnit4Settings();
            }
        }
        catch (Exception e) when (e is InvalidOperationException or XmlException)
        {
            // the run settings section is malformed (XmlException) or does not match the settings type (InvalidOperationException), use the defaults
            Console.WriteLine($"Loading GdUnit4 Adapter settings failed! {e}");
        }
    }

    internal static GdUnit4Settings LoadSettings(IDiscoveryContext discoveryContext)
    {
        var gdUnitSettingsProvider = discoveryContext.RunSettings?.GetSettings(GdUnit4Settings.RUN_SETTINGS_XML_NODE) as GdUnit4SettingsProvider;
        return gdUnitSettingsProvider?.Settings ?? new GdUnit4Settings();
    }
}
