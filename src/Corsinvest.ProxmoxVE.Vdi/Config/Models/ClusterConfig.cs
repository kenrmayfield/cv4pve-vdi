/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.Config.Models;

internal class ClusterConfig
{
    /// <summary>
    /// Display name for this host/cluster
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hosts for HA failover: host:port,host:port
    /// e.g. "pve1.example.com:8006,pve2.example.com:8006"
    /// </summary>
    public string Hosts { get; set; } = string.Empty;

    /// <summary>
    /// Skip TLS certificate validation
    /// </summary>
    public bool SkipSslValidation { get; set; } = false;

    /// <summary>
    /// Connection timeout in seconds (default 10)
    /// </summary>
    public int Timeout { get; set; } = 10;

    /// <summary>
    /// SPICE connection options for this cluster.
    /// </summary>
    public SpiceConfig Spice { get; set; } = new();

    /// <summary>
    /// Per-VM/CT service configuration.
    /// </summary>
    public List<VmConfig> Vms { get; set; } = [];
}
