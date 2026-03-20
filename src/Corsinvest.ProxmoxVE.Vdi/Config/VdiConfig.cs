/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using YamlDotNet.Serialization;

namespace Corsinvest.ProxmoxVE.Vdi.Config;

internal class VdiConfig
{
    public const string ThemeSystem = "System";
    public const string ThemeLight = "Light";
    public const string ThemeDark = "Dark";

    public const string ViewCard = "Card";
    public const string ViewList = "List";

    /// <summary>Default view: Card or List</summary>
    public string DefaultView { get; set; } = ViewCard;

    [YamlIgnore]
    public ThemeVariant ThemeVariant
        => Theme switch
        {
            ThemeLight => ThemeVariant.Light,
            ThemeDark => ThemeVariant.Dark,
            _ => ThemeVariant.Default
        };

    /// <summary>
    /// Last username used at login (pre-populated in login form)
    /// </summary>
    public string LastUser { get; set; } = string.Empty;

    /// <summary>
    /// Path to remote-viewer / virt-viewer executable for SPICE
    /// </summary>
    public string ViewerPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to RDP client executable (mstsc.exe on Windows, xfreerdp on Linux/Mac)
    /// Leave empty to use the system default
    /// </summary>
    public string RdpPath { get; set; } = string.Empty;

    /// <summary>
    /// UI theme: System, Light, Dark
    /// </summary>
    public string Theme { get; set; } = ThemeDark;

    /// <summary>
    /// List of PVE clusters/hosts
    /// </summary>
    public List<VdiHost> Hosts { get; set; } = [];

    // Appearance

    /// <summary>Show Start button in the Actions column (default false)</summary>
    public bool ShowStartButton { get; set; } = false;

    /// <summary>Show Shutdown button in the Actions column (default false)</summary>
    public bool ShowShutdownButton { get; set; } = false;

    /// <summary>Ask confirmation before starting a VM (default false)</summary>
    public bool ConfirmStart { get; set; } = false;

    /// <summary>Ask confirmation before shutting down a VM (default false)</summary>
    public bool ConfirmShutdown { get; set; } = false;

    /// <summary>Show CPU/RAM progress bars in card and list views (default true)</summary>
    public bool ShowBars { get; set; } = true;

    /// <summary>Show node filter section in sidebar (default false)</summary>
    public bool ShowNodes { get; set; } = false;

    /// <summary>Show pool filter in sidebar and pool info in card/list (default false)</summary>
    public bool ShowPools { get; set; } = false;

    /// <summary>Show tag badges in card/list and tag filter in sidebar (default true)</summary>
    public bool ShowTags { get; set; } = true;

    /// <summary>Enable SPICE console (default true)</summary>
    public bool EnableSpice { get; set; } = true;

    /// <summary>Enable VNC console (default true)</summary>
    public bool EnableVnc { get; set; } = true;

    /// <summary>Enable RDP detection (default false — requires IP scan)</summary>
    public bool EnableRdp { get; set; } = false;

    /// <summary>Ping QEMU guest agent to detect if running (default false)</summary>
    public bool EnableAgentPing { get; set; } = false;
}
