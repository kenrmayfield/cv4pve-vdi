/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.Config.Models;

/// <summary>
/// Service configuration for a specific VM/CT.
/// </summary>
internal class VmConfig
{
    /// <summary>VM/CT identifier.</summary>
    public int VmId { get; set; }

    /// <summary>Services configured for this VM/CT.</summary>
    public List<VmServiceConfig> Services { get; set; } = [];
}
