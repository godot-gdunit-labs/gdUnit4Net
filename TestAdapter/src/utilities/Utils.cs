// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter.Utilities;

using System;
using System.IO;
using System.Linq;

internal static class Utils
{
    internal static string GetUserDataDirectory => Environment.OSVersion.Platform switch
    {
        PlatformID.Win32NT => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Godot"),
        PlatformID.Unix => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "godot"),
        PlatformID.MacOSX => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library/Application Support/Godot"),
        PlatformID.Win32S => throw new NotImplementedException(),
        PlatformID.Win32Windows => throw new NotImplementedException(),
        PlatformID.WinCE => throw new NotImplementedException(),
        PlatformID.Xbox => throw new NotImplementedException(),
        PlatformID.Other => throw new NotImplementedException(),
        _ => throw new PlatformNotSupportedException("Unsupported operating system")
    };

    internal static string GetProjectDirectory
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null &&
                   !directory.EnumerateFiles("*.sln").Any() &&
                   !directory.EnumerateFiles("*.csproj").Any())
                directory = directory.Parent;

            return directory?.FullName ?? throw new FileNotFoundException($"Could not find project root directory {directory}");
        }
    }
}
