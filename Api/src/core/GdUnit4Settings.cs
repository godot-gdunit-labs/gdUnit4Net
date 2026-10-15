// Copyright (c) 2025 Mike Schulze
// MIT License - See LICENSE file in the repository root for full license text

namespace GdUnit4.Core;

using Extensions;

using Godot;

/// <summary>
///     Gets access to the GdUnit4 settings to be configured in the Godot editor.
/// </summary>
internal static class GdUnit4Settings
{
    private const string MAIN_CATEGORY = "gdunit4";

    // Common Settings
    private const string COMMON_SETTINGS = MAIN_CATEGORY + "/settings";

    private const string GROUP_COMMON = COMMON_SETTINGS + "/common";
    private const string UPDATE_NOTIFICATION_ENABLED = GROUP_COMMON + "/update_notification_enabled";
    private const string SERVER_TIMEOUT = GROUP_COMMON + "/server_connection_timeout_minutes";

    private const string GROUP_TEST = COMMON_SETTINGS + "/test";
    private const string TEST_TIMEOUT = GROUP_TEST + "/test_timeout_seconds";

    private const string TEST_LOOKUP_FOLDER = GROUP_TEST + "/test_lookup_folder";

    // Report Settings
    private const string REPORT_SETTINGS = MAIN_CATEGORY + "/report";
    private const string GROUP_GODOT = REPORT_SETTINGS + "/godot";
    private const string REPORT_ORPHANS = REPORT_SETTINGS + "/verbose_orphans";
    private const string REPORT_PUSH_ERRORS = GROUP_GODOT + "/push_error";
    private const string REPORT_SCRIPT_ERRORS = GROUP_GODOT + "/script_error";
    private const string GROUP_ASSERT = REPORT_SETTINGS + "/assert";
    private const string REPORT_ASSERT_WARNINGS = GROUP_ASSERT + "/verbose_warnings";
    private const string REPORT_ASSERT_ERRORS = GROUP_ASSERT + "/verbose_errors";
    private const string REPORT_ASSERT_STRICT_NUMBER_TYPE_COMPARE = GROUP_ASSERT + "/strict_number_type_compare";

    // Godot debug stdout/logging settings
    private const string CATEGORY_LOGGING = "debug/file_logging/";
    private const string STDOUT_ENABLE_TO_FILE = CATEGORY_LOGGING + "enable_file_logging";
    private const string STDOUT_WRITE_TO_FILE = CATEGORY_LOGGING + "log_path";

    // UI Settings
    private const string UI_SETTINGS = MAIN_CATEGORY + "/ui";
    private const string GROUP_UI_INSPECTOR = UI_SETTINGS + "/inspector";
    private const string INSPECTOR_NODE_COLLAPSE = GROUP_UI_INSPECTOR + "/node_collapse";

    // Toolbar Settings
    private const string GROUP_UI_TOOLBAR = UI_SETTINGS + "/toolbar";

    private const string INSPECTOR_TOOLBAR_BUTTON_RUN_OVERALL = GROUP_UI_TOOLBAR + "/run_overall";

    // defaults
    // server connection timeout in minutes
    private const int DEFAULT_SERVER_TIMEOUT = 30;

    // test case runtime timeout in seconds
    private const int DEFAULT_TEST_TIMEOUT = 60 * 5;

    // the folder to create new test-suites
    private const string DEFAULT_TEST_LOOKUP_FOLDER = "test";

    public static bool IsUpdateNotificationEnabled()
        => ProjectSettings.HasSetting(UPDATE_NOTIFICATION_ENABLED) && (bool)ProjectSettings.GetSetting(UPDATE_NOTIFICATION_ENABLED);

    public static void SetUpdateNotification(bool enable)
    {
        ProjectSettings.SetSetting(UPDATE_NOTIFICATION_ENABLED, enable);
        _ = ProjectSettings.Save();
    }

    public static string GetLogPath()
        => (string)ProjectSettings.GetSetting(STDOUT_WRITE_TO_FILE);

    public static void SetLogPath(string path)
    {
        ProjectSettings.SetSetting(STDOUT_ENABLE_TO_FILE, true);
        ProjectSettings.SetSetting(STDOUT_WRITE_TO_FILE, path);
        _ = ProjectSettings.Save();
    }

    // the configured server connection timeout in ms
    public static int ServerTimeout()
        => GetSetting(SERVER_TIMEOUT, DEFAULT_SERVER_TIMEOUT) * 60 * 1000;

    // the configured test case timeout in ms
    public static int TestTimeout()
        => GetSetting(TEST_TIMEOUT, DEFAULT_TEST_TIMEOUT) * 1000;

    // the root folder to store/generate test-suites
    public static string? TestRootFolder()
        => GetSetting(TEST_LOOKUP_FOLDER, DEFAULT_TEST_LOOKUP_FOLDER);

    public static bool IsVerboseAssertWarnings()
        => GetSetting(REPORT_ASSERT_WARNINGS, true);

    public static bool IsVerboseAssertErrors()
        => GetSetting(REPORT_ASSERT_ERRORS, true);

    public static bool IsVerboseOrphans()
        => GetSetting(REPORT_ORPHANS, true);

    public static bool IsStrictNumberTypeCompare()
        => GetSetting(REPORT_ASSERT_STRICT_NUMBER_TYPE_COMPARE, true);

    public static bool IsReportPushErrors()
        => GetSetting(REPORT_PUSH_ERRORS, false);

    public static bool IsReportScriptErrors()
        => GetSetting(REPORT_SCRIPT_ERRORS, true);

    public static bool IsInspectorNodeCollapse()
        => GetSetting(INSPECTOR_NODE_COLLAPSE, true);

    public static bool IsInspectorToolbarButtonShow()
        => GetSetting(INSPECTOR_TOOLBAR_BUTTON_RUN_OVERALL, true);

    public static bool IsLogEnabled()
        => (bool)ProjectSettings.GetSetting(STDOUT_ENABLE_TO_FILE);

    private static T? GetSetting<T>(string name, T @default)
        => ProjectSettings.HasSetting(name)
            ? ProjectSettings.GetSetting(name).UnboxVariant()
            : @default;
}
