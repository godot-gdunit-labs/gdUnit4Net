// Copyright (c) 2026 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.TestAdapter.Extensions;

using System.Text;

using Api;

using static Api.ReportType;

internal static class StringExtensions
{
    private const string ANSI_RESET = "\u001b[0m";
    private const string ANSI_BLUE = "\u001b[34m";
    private const string ANSI_YELLOW = "\u001b[33m";
    private const string ANSI_BOLD = "\u001b[1m";
    private const string ANSI_ITALIC = "\u001b[3m";

    public static string Indent(this string str, int count = 1, string indentWith = "\t")
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var indent = string.Concat(Enumerable.Repeat(indentWith, count));
        return indent + str.Replace("\n", "\n" + indent, StringComparison.Ordinal);
    }

    public static string FormatMessageColored(this string message, ReportType reportType)
    {
        var sb = new StringBuilder();

        // Header line (always visible)
        switch (reportType)
        {
            case Stdout:
                _ = sb.AppendLine($"{ANSI_BLUE}{ANSI_BOLD}Standard Output: {ANSI_ITALIC}{ANSI_RESET}");
                _ = sb.AppendLine($"{ANSI_BLUE}──────────────────────────────────────────{ANSI_RESET}");

                break;
            case Warning:
                _ = sb.AppendLine($"{ANSI_YELLOW}{ANSI_BOLD}Warning:{ANSI_ITALIC}{ANSI_RESET}");

                break;
            case Success:
            case Failure:
            case Orphan:
            case Terminated:
            case Interrupted:
            case Abort:
            case Skipped:
            default:
                break;
        }

        _ = sb
            .Append(message)
            .Append(Environment.NewLine);
        return sb.ToString();
    }
}
