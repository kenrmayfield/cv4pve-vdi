/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.UI.Models;

internal record VmFeatures(
    bool Audio,
    bool UsbRedirect,
    bool AgentConfigured,
    bool? AgentRunning,
    bool Clipboard)
{
    public static readonly VmFeatures None = new(false, false, false, null, false);
}
