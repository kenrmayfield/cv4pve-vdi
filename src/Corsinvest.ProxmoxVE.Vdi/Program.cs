/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.UI;

internal sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var config = VdiConfigManager.Load();

        Window Build() => LoginWindow.Create(config);

        AppBuilder.Configure<Application>()
                  .UsePlatformDetect()
                  .WithApplicationName("cv4pve-vdi")
                  .UseFluentTheme()
                  .AfterSetup(b =>
                  {
                      if (b.Instance == null) { return; }

                      b.Instance.RequestedThemeVariant = config.ThemeVariant;
                  })
                  .StartWithClassicDesktopLifetime(Build, args);
    }
}
