/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class HostEditWindow
{
    /// <summary>
    /// Pass null for Add, or an existing VdiHost for Edit.
    /// Returns the saved VdiHost or null if cancelled.
    /// </summary>
    public static Window Create(VdiHost? existing)
    {
        var isEdit = existing != null;

        var txtName = new TextBox
        {
            Text = existing?.Name ?? string.Empty,
            Watermark = "e.g. prod",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var txtUrl = new TextBox
        {
            Text = existing?.Hosts ?? string.Empty,
            Watermark = "pve1.example.com:8006",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var chkSkipSsl = new CheckBox
        {
            Content = "Skip TLS certificate validation",
            IsChecked = existing?.SkipSslValidation ?? false
        };

        var numTimeout = new NumericUpDown
        {
            Value = existing?.Timeout ?? 10,
            Minimum = 5,
            Maximum = 120,
            Increment = 5,
            FormatString = "0",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var txtProxy = new TextBox
        {
            Text = existing?.Spice.Proxy ?? string.Empty,
            Watermark = "host:port or https://host:port (empty = use PVE host)",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var txtViewerOptions = new TextBox
        {
            Text = existing?.Spice.ViewerOptions ?? string.Empty,
            Watermark = "extra args for remote-viewer...",
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var lblError = new TextBlock
        {
            Foreground = Brushes.Red,
            IsVisible = false
        };

        var btnSave = new Button { Content = isEdit ? Icons.WithText(Icons.Save, "Save") : Icons.WithText(Icons.Add, "Add") };
        var btnCancel = new Button { Content = Icons.WithText(Icons.Close, "Cancel") };

        var window = new Window
        {
            Title = isEdit ? "Edit Host" : "Add Host",
            Width = 420,
            CanResize = false,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = "Name", FontWeight = FontWeight.Bold },
                    txtName,
                    new TextBlock { Text = "Hosts (host:port,host:port for HA)", FontWeight = FontWeight.Bold },
                    txtUrl,
                    chkSkipSsl,
                    new TextBlock { Text = "Timeout (seconds)", FontWeight = FontWeight.Bold },
                    numTimeout,
                    new TextBlock { Text = "SPICE proxy (optional)", FontWeight = FontWeight.Bold },
                    txtProxy,
                    new TextBlock { Text = "Viewer extra options (optional)", FontWeight = FontWeight.Bold },
                    txtViewerOptions,
                    lblError,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { btnCancel, btnSave }
                    }
                }
            }
        };

        btnSave.Click += (_, _) =>
        {
            var name = txtName.Text?.Trim() ?? string.Empty;
            var url = txtUrl.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url))
            {
                lblError.Text = "Name and URL are required.";
                lblError.IsVisible = true;
                return;
            }

            var result = new VdiHost
            {
                Name = name,
                Hosts = url,
                SkipSslValidation = chkSkipSsl.IsChecked == true,
                Timeout = (int)(numTimeout.Value ?? 10),
                Spice = new SpiceOptions
                {
                    Proxy = txtProxy.Text?.Trim() ?? string.Empty,
                    ViewerOptions = txtViewerOptions.Text?.Trim() ?? string.Empty
                }
            };

            window.Close(result);
        };

        btnCancel.Click += (_, _) => window.Close(null);

        return window;
    }
}
