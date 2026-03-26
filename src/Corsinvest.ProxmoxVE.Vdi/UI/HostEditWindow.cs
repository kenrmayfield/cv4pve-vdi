/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class HostEditWindow
{
    /// <summary>
    /// Pass null for Add, or an existing ClusterConfig for Edit.
    /// Returns the saved ClusterConfig or null if cancelled.
    /// </summary>
    public static Window Create(ClusterConfig? existing)
    {
        var isEdit = existing != null;

        var txtName = new TextBox
        {
            Text = existing?.Name ?? string.Empty,
            Watermark = "e.g. prod",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Server)
        };

        var txtUrl = new TextBox
        {
            Text = existing?.Hosts ?? string.Empty,
            Watermark = "pve1.example.com:8006",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Network)
        };

        var chkSkipSsl = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.Lock, L("HostSkipSsl")),
            IsChecked = existing?.SkipSslValidation ?? false
        };

        var numTimeout = new NumericUpDown
        {
            Value = existing?.Timeout ?? 10,
            Minimum = 5,
            Maximum = 120,
            Increment = 5,
            FormatString = "0",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(24, 4, 0, 4)
        };
        var numTimeoutWithIcon = new Grid();
        numTimeoutWithIcon.Children.Add(numTimeout);
        numTimeoutWithIcon.Children.Add(AppIcons.InnerOverlay(AppIcons.Clock));

        var txtProxy = new TextBox
        {
            Text = existing?.Spice.Proxy ?? string.Empty,
            Watermark = "host:port or https://host:port (empty = use PVE host)",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Ethernet)
        };

        var txtViewerOptions = new TextBox
        {
            Text = existing?.Spice.ViewerOptions ?? string.Empty,
            Watermark = L("HostViewerOptionsWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Console)
        };

        var lblError = new TextBlock
        {
            Foreground = Brushes.Red,
            IsVisible = false
        };

        var btnSave = new Button
        {
            Content = AppIcons.Toolbar(isEdit ? AppIcons.Save : AppIcons.Add),
            Padding = new Thickness(6, 4),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };
        Avalonia.Controls.ToolTip.SetTip(btnSave, isEdit ? L("Save") : L("Add"));

        var window = new Window
        {
            Title = isEdit
                    ? L("EditHost")
                    : L("AddHost"),

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
                    new TextBlock
                    {
                        Text = L("HostName"),
                        FontWeight = FontWeight.Bold
                    },
                    txtName,
                    new TextBlock
                    {
                        Text = L("HostHosts"),
                        FontWeight = FontWeight.Bold
                    },
                    txtUrl,
                    chkSkipSsl,
                    new TextBlock
                    {
                        Text = L("HostTimeout"),
                        FontWeight = FontWeight.Bold
                    },
                    numTimeoutWithIcon,
                    new TextBlock
                    {
                        Text = L("HostSpiceProxy"),
                        FontWeight = FontWeight.Bold
                    },
                    txtProxy,
                    new TextBlock
                    {
                        Text = L("HostViewerOptions"),
                        FontWeight = FontWeight.Bold
                    },
                    txtViewerOptions,
                    lblError,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { btnSave }
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
                lblError.Text = L("HostNameRequired");
                lblError.IsVisible = true;
                return;
            }

            var result = new ClusterConfig
            {
                Name = name,
                Hosts = url,
                SkipSslValidation = chkSkipSsl.IsChecked == true,
                Timeout = (int)(numTimeout.Value ?? 10),
                Spice = new SpiceConfig
                {
                    Proxy = txtProxy.Text?.Trim() ?? string.Empty,
                    ViewerOptions = txtViewerOptions.Text?.Trim() ?? string.Empty
                }
            };

            window.Close(result);
        };

        return window;
    }
}
