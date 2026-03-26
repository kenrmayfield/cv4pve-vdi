/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.Config.Models;

/// <summary>
/// Configuration for a service (RDP, SSH, etc.) on a specific VM/CT.
/// </summary>
internal class VmServiceConfig
{
    /// <summary>References a launcher serviceId (e.g. "rdp-mstsc", "ssh-putty").</summary>
    public string ServiceId { get; set; } = string.Empty;

    /// <summary>Port to connect to. Overrides the launcher's default port.</summary>
    public int Port { get; set; }

    /// <summary>Where to source the credentials from.</summary>
    public CredentialSource CredentialSource { get; set; } = CredentialSource.None;

    /// <summary>Credentials used when CredentialSource is Manual.</summary>
    public Credentials? Credentials { get; set; }

    /// <summary>Fixed IP/hostname override. If empty, IP is resolved automatically.</summary>
    public string IpOverride { get; set; } = string.Empty;

    /// <summary>Extra arguments override for this specific service. Overrides the launcher's extraArgs.</summary>
    public string ExtraArgs { get; set; } = string.Empty;
}
