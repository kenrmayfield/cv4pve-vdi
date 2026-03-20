/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.UI.Models;

/// <summary>
/// SPICE/agent features detected from VM config and runtime state.
/// </summary>
internal record SpiceFeatures(
    bool Audio,
    bool UsbRedirect,
    bool AgentConfigured,
    bool AgentRunning,
    bool Clipboard)
{
    public static readonly SpiceFeatures None = new(false, false, false, false, false);
}
