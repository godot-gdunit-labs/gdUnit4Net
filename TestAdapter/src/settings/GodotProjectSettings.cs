// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter.Settings;

using System;
using System.IO;
using System.Linq;

using Utilities;

internal class GodotProjectSettings
{
    internal ApplicationSection Application { get; set; } = new();

    internal DebugSection Debug { get; set; } = new();

    internal EditorPluginsSection EditorPlugins { get; set; } = new();

    internal GdUnit4Section GdUnit4 { get; set; } = new();

    internal static GodotProjectSettings LoadFromFile(string filePath)
    {
        var settings = new GodotProjectSettings();
        var lines = File.ReadAllLines(filePath);

        var currentSection = string.Empty;
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith(';'))
                continue;

            if (trimmedLine.StartsWith('[') && trimmedLine.EndsWith(']'))
            {
                currentSection = trimmedLine[1..^1];
                continue;
            }

            if (string.IsNullOrEmpty(currentSection) || !trimmedLine.Contains('=', StringComparison.Ordinal))
                continue;

            var parts = trimmedLine.Split(['='], 2);
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = ParseValue(parts[1].Trim());
                SetPropertyValue(settings, currentSection, key, value);
            }
        }

        // fix godot paths to system paths
        settings.Debug.FileLogging.LogPath =
            GlobalizeGodotPath(settings.Debug.FileLogging.LogPath, settings.Application.Config.Name);

        return settings;
    }

    internal static string GlobalizeGodotPath(string path, string projectName)
    {
        if (!path.StartsWith("user://") && !path.StartsWith("res://"))
            return path;

        return path
            .Replace("user:/", Path.Combine(Utils.GetUserDataDirectory, "app_userdata", projectName), StringComparison.OrdinalIgnoreCase)
            .Replace("res:/", Utils.GetProjectDirectory, StringComparison.OrdinalIgnoreCase)
            .Replace("/", Path.DirectorySeparatorChar.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static void SetPropertyValue(GodotProjectSettings settings, string section, string key, object value)
    {
        object? sectionObj = section.ToLower() switch
        {
            "application" => settings.Application,
            "debug" => settings.Debug,
            "editor_plugins" => settings.EditorPlugins,
            "gdunit4" => settings.GdUnit4,
            _ => null
        };

        if (sectionObj == null)
            return;

        var propertyParts = key.Split('/');
        var currentObj = sectionObj;

        for (var i = 0; i < propertyParts.Length - 1; i++)
        {
            var propertyName = ToPascalCase(propertyParts[i]);
            var property = currentObj.GetType().GetProperty(propertyName);

            if (property == null)
                return;

            currentObj = property.GetValue(currentObj);
            if (currentObj == null)
                return;
        }

        var finalPropertyName = ToPascalCase(propertyParts[^1]);
        var finalProperty = currentObj.GetType().GetProperty(finalPropertyName);

        if (finalProperty != null)
        {
            try
            {
                if (finalProperty.PropertyType.IsArray)
                {
                    if (value is string[] arrayValue)
                        finalProperty.SetValue(currentObj, arrayValue);
                }
                else
                {
                    var convertedValue = Convert.ChangeType(value, finalProperty.PropertyType);
                    finalProperty.SetValue(currentObj, convertedValue);
                }
            }
            catch (Exception e) when (e is InvalidCastException or FormatException or OverflowException)
            {
                // the value does not convert to the property type, keep the default of the setting
            }
        }
    }

    private static string ToPascalCase(string input)
    {
        var words = input.Split('_', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(
            words.Select(word =>
                char.ToUpper(word[0]) +
                (word.Length > 1 ? word[1..].ToLower() : string.Empty)));
    }

    private static object ParseValue(string value)
    {
        if (value.StartsWith('"') && value.EndsWith('"'))
            return value[1..^1];

        if (value.StartsWith("PackedStringArray(") && value.EndsWith(')'))
        {
            var arrayContent = value.Substring("PackedStringArray(".Length, value.Length - "PackedStringArray(".Length - 1);
            return arrayContent.Split(',')
                .Select(s => s.Trim().Trim('"'))
                .ToArray();
        }

        if (bool.TryParse(value.ToLower(), out var boolResult))
            return boolResult;

        if (double.TryParse(value, out var doubleResult))
            return doubleResult;

        return value;
    }

#pragma warning disable SA1201, SA1600
    public interface ISettingsSection
#pragma warning restore SA1201, SA1600
    {
    }

    public class ApplicationSection : ISettingsSection
    {
        public ConfigSetting Config { get; set; } = new();

        public class ConfigSetting
        {
            public string Name { get; set; } = string.Empty;

            public string[] Tags { get; set; } = [];

            public string[] Features { get; set; } = [];

            public string Icon { get; set; } = string.Empty;
        }
    }

    public class DebugSection : ISettingsSection
    {
        public FileLoggingSettings FileLogging { get; set; } = new();

        public class FileLoggingSettings
        {
            public bool EnableFileLogging { get; set; }

            public string LogPath { get; set; } = "user://logs/godot.log";
        }
    }

    public class EditorPluginsSection : ISettingsSection
    {
        public string[] Enabled { get; set; } = [];
    }

    public class GdUnit4Section : ISettingsSection
    {
        public TestRunSettings Settings { get; set; } = new();

        public ReportSettings Report { get; set; } = new();

        public class TestRunSettings
        {
            public TestSettings Test { get; set; } = new();

            public class TestSettings
            {
                public bool FlakyCheckEnable { get; set; }
            }
        }

        public class ReportSettings
        {
            public GodotReportSettings Godot { get; set; } = new();

            public class GodotReportSettings
            {
                public bool ScriptError { get; set; }
            }
        }
    }
}
