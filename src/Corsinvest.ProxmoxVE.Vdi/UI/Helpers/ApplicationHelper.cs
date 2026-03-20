/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Reflection;

namespace Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

internal static class ApplicationHelper
{
    public static readonly string Version =
        Assembly.GetEntryAssembly()
                ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion.Split('+')[0]
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        ?? "?.?.?";

    public const string GitHubRepo = "https://github.com/Corsinvest/cv4pve-vdi";
    public const string DocumentationUrl = "https://github.com/Corsinvest/cv4pve-vdi#readme";
    public const string ReleaseNotesUrl = "https://github.com/Corsinvest/cv4pve-vdi/releases";
    public const string SupportUrl = "https://github.com/Corsinvest/cv4pve-vdi/issues";
    public static string GetBugReportUrl(string pveVersion)
    {
        var os = Environment.OSVersion.Platform switch
        {
            PlatformID.Win32NT => "Windows",
            PlatformID.Unix => "Linux",
            PlatformID.MacOSX => "macOS",
            _ => "Linux"
        };
        var url = $"{GitHubRepo}/issues/new?template=bug_report.yml" +
                  $"&version={Uri.EscapeDataString(Version)}" +
                  $"&os={Uri.EscapeDataString(os)}";
        if (!string.IsNullOrEmpty(pveVersion)) { url += $"&pve-version={Uri.EscapeDataString(pveVersion)}"; }
        return url;
    }

    public static string FeatureRequestUrl => $"{GitHubRepo}/issues/new?template=feature_request.yml";
}
