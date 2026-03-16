/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Vdi.Config;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class MainWindow
{
    public static Window Create(PveClient client, VdiHost host, VdiConfig config)
        => new MainWindowContext(client, host, config).Build();
}
