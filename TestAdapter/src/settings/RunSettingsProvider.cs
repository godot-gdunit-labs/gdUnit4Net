// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter.Settings;

using System.Xml;

using Microsoft.VisualStudio.TestPlatform.ObjectModel;

/// <summary>
///     Provides functionality to read and parse RunSettings configuration for test execution.
/// </summary>
/// <remarks>
///     This class handles XML-based RunSettings files to extract settings like environment variables.
///     The settings are used to configure the test environment before test execution.
/// </remarks>
public static class RunSettingsProvider
{
    /// <summary>
    ///     Gets the settings to be used while creating XmlReader for runsettings.
    /// </summary>
    private static XmlReaderSettings ReaderSettings => new()
    {
        IgnoreComments = true,
        IgnoreWhitespace = true,
        DtdProcessing = DtdProcessing.Prohibit
    };

    /// <summary>
    ///     Extracts environment variables defined in RunSettings configuration.
    /// </summary>
    /// <param name="settingsXml">The RunSettings XML content.</param>
    /// <returns>Dictionary of environment variable names and values.</returns>
    /// <exception cref="SettingsException">Thrown when XML parsing fails.</exception>
    public static Dictionary<string, string> GetEnvironmentVariables(string? settingsXml)
    {
        if (string.IsNullOrEmpty(settingsXml))
            return [];

        using var stringReader = new StringReader(settingsXml);
        using var reader = XmlReader.Create(stringReader, ReaderSettings);
        var envVars = new Dictionary<string, string>();

        // Try to navigate to EnvironmentVariables section
        if (!reader.ReadToDescendant("EnvironmentVariables"))
            return envVars;

        // over all EnvironmentVariables
        using var variables = reader.ReadSubtree();
        _ = variables.MoveToContent();
        _ = variables.Read(); // Move past the EnvironmentVariables element
        while (!variables.EOF)
        {
            if (variables.IsStartElement())
                envVars[variables.Name] = variables.ReadElementContentAsString();
            else
                _ = variables.Read();
        }

        return envVars;
    }
}
