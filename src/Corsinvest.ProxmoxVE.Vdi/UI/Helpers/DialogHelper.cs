/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

namespace Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

internal static class DialogHelper
{
    public static async Task<bool> ConfirmAsync(Window owner, string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        var dlg = new Window
        {
            Title = L("Confirm"),
            Width = 320,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var btnYes = new Button { Content = L("Yes"), Width = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
        var btnNo = new Button { Content = L("No"), Width = 80, HorizontalContentAlignment = HorizontalAlignment.Center };

        btnYes.Click += (_, _) => { tcs.TrySetResult(true); dlg.Close(); };
        btnNo.Click += (_, _) => { tcs.TrySetResult(false); dlg.Close(); };
        dlg.Closed += (_, _) => tcs.TrySetResult(false);

        dlg.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                new StackPanel
                {
                    Orientation         = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing             = 12,
                    Children            = { btnYes, btnNo }
                }
            }
        };

        await dlg.ShowDialog(owner);
        return await tcs.Task;
    }
}
