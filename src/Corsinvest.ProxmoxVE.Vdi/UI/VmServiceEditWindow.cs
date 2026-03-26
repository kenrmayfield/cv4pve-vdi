/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class VmServiceEditWindow
{
    public static async Task<VmServiceConfig?> ShowAsync(
        Window owner,
        VmServiceConfig? existing,
        IReadOnlyList<LauncherDefinition> launchers)
    {
        var isEdit = existing is not null;

        // Launcher combo
        var cmbLauncher = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = launchers,
            DisplayMemberBinding = new Avalonia.Data.Binding("DisplayName")
        };

        if (isEdit)
        {
            var idx = launchers.ToList().FindIndex(l => l.ServiceId == existing!.ServiceId);
            cmbLauncher.SelectedIndex = idx >= 0 ? idx : 0;
        }
        else if (launchers.Count > 0)
        {
            cmbLauncher.SelectedIndex = 0;
        }

        // Port
        var numPort = new NumericUpDown
        {
            Minimum = 1,
            Maximum = 65535,
            Increment = 1,
            FormatString = "0",
            Value = existing?.Port > 0
                            ? existing.Port
                            : (launchers.Count > 0 ? launchers[0].DefaultPort : 22),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(24, 4, 0, 4)
        };
        var numPortWithIcon = new Grid();
        numPortWithIcon.Children.Add(numPort);
        numPortWithIcon.Children.Add(AppIcons.InnerOverlay(AppIcons.Ethernet));

        cmbLauncher.SelectionChanged += (_, _) =>
        {
            if (cmbLauncher.SelectedItem is LauncherDefinition def && !isEdit)
            {
                numPort.Value = def.DefaultPort;
            }
        };

        // Credential source
        var cmbCredSource = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = Enum.GetValues<CredentialSource>(),
            SelectedItem = existing?.CredentialSource ?? CredentialSource.None
        };

        // IP override
        var txtIpOverride = new TextBox
        {
            Text = existing?.IpOverride ?? string.Empty,
            Watermark = L("IpOverrideWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Network)
        };

        // Extra args override
        var txtExtraArgs = new TextBox
        {
            Text = existing?.ExtraArgs ?? string.Empty,
            Watermark = L("ExtraArgsWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Console)
        };

        // Credentials (shown only when CredentialSource.Config)
        var txtUsername = new TextBox
        {
            Text = existing?.Credentials?.Username ?? string.Empty,
            Watermark = L("Username"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Account)
        };

        var txtPassword = new TextBox
        {
            Text = existing?.Credentials?.Password ?? string.Empty,
            Watermark = L("Password"),
            PasswordChar = '●',
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Lock)
        };

        var credPanel = new StackPanel
        {
            Spacing = 8,
            IsVisible = existing?.CredentialSource == CredentialSource.Manual,
            Children = { txtUsername, txtPassword }
        };

        cmbCredSource.SelectionChanged += (_, _) =>
        {
            credPanel.IsVisible = cmbCredSource.SelectedItem is CredentialSource.Manual;
        };

        // Error / Save
        var lblError = new TextBlock { Foreground = Brushes.Red, IsVisible = false };

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
            Title = isEdit ? L("EditService") : L("AddService"),
            Width = 380,
            CanResize = false,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = L("Launcher"), FontWeight = FontWeight.Bold },
                    cmbLauncher,
                    new TextBlock { Text = L("Port"), FontWeight = FontWeight.Bold },
                    numPortWithIcon,
                    new TextBlock { Text = L("IpOverride"), FontWeight = FontWeight.Bold },
                    txtIpOverride,
                    new TextBlock { Text = L("ExtraArgs"), FontWeight = FontWeight.Bold },
                    txtExtraArgs,
                    new TextBlock { Text = L("CredentialSource"), FontWeight = FontWeight.Bold },
                    cmbCredSource,
                    credPanel,
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
            if (cmbLauncher.SelectedItem is not LauncherDefinition launcher)
            {
                lblError.Text = L("SelectLauncher");
                lblError.IsVisible = true;
                return;
            }

            var credSource = cmbCredSource.SelectedItem is CredentialSource cs
                                ? cs
                                : CredentialSource.None;

            Credentials? credentials = null;
            if (credSource == CredentialSource.Manual)
            {
                credentials = new Credentials
                {
                    Username = txtUsername.Text?.Trim() ?? string.Empty,
                    Password = txtPassword.Text?.Trim() ?? string.Empty
                };
            }

            window.Close(new VmServiceConfig
            {
                ServiceId = launcher.ServiceId,
                Port = (int)(numPort.Value ?? launcher.DefaultPort),
                IpOverride = txtIpOverride.Text?.Trim() ?? string.Empty,
                ExtraArgs = txtExtraArgs.Text?.Trim() ?? string.Empty,
                CredentialSource = credSource,
                Credentials = credentials
            });
        };

        return await window.ShowDialog<VmServiceConfig?>(owner);
    }
}
