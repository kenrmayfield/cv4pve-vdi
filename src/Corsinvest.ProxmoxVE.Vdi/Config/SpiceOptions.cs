/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.Config;

internal class SpiceOptions
{
    /// <summary>
    /// Proxy override for this cluster. If empty, the PVE host is used.
    /// Can be host:port or http(s)://host:port for reverse proxy.
    /// </summary>
    public string Proxy { get; set; } = string.Empty;

    /// <summary>
    /// Extra arguments passed to remote-viewer for this cluster.
    /// </summary>
    public string ViewerOptions { get; set; } = string.Empty;
}
