/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class LauncherEditWindow
{
    /// <summary>
    /// Pass null for <paramref name="existing"/> to create a new launcher.
    /// Returns the saved LauncherDefinition or null if cancelled.
    /// </summary>
    public static async Task<LauncherDefinition?> ShowAsync(Window owner, LauncherDefinition? existing = null)
    {
        var isNew = existing is null;

        // Service ID
        var txtServiceId = new TextBox
        {
            Text = existing?.ServiceId ?? string.Empty,
            Watermark = "e.g. my-rdp-client",
            IsReadOnly = !isNew,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Tag)
        };

        // Display name
        var txtDisplayName = new TextBox
        {
            Text = existing?.DisplayName ?? string.Empty,
            Watermark = "e.g. My RDP Client",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Edit)
        };

        // Platform
        var cmbPlatform = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemsSource = Enum.GetValues<LauncherPlatform>(),
            SelectedItem = existing?.Platform ?? LauncherPlatform.Windows,
            Padding = new Thickness(24, 4, 0, 4)
        };
        var cmbPlatformWithIcon = new Grid();
        cmbPlatformWithIcon.Children.Add(cmbPlatform);
        cmbPlatformWithIcon.Children.Add(AppIcons.InnerOverlay(AppIcons.Server));

        // Default port
        var numPort = new NumericUpDown
        {
            Value = existing?.DefaultPort ?? 22,
            Minimum = 1,
            Maximum = 65535,
            Increment = 1,
            FormatString = "0",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(24, 4, 0, 4)
        };
        var numPortWithIcon = new Grid();
        numPortWithIcon.Children.Add(numPort);
        numPortWithIcon.Children.Add(AppIcons.InnerOverlay(AppIcons.Ethernet));

        // Executable
        var txtExecutable = new TextBox
        {
            Text = existing?.Executable ?? string.Empty,
            Watermark = L("ExeWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Console)
        };

        // Arguments
        // {ip}:{port} {?{username}} {extraArgs}
        // {?TEXT} = includi TEXT (con token risolti) solo se tutti i token interni sono non vuoti
        var txtArguments = new TextBox
        {
            Text = existing?.Arguments ?? string.Empty,
            Watermark = "{ip}:{port} {?{username}} {extraArgs}",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Console)
        };

        // Extra args
        var txtExtraArgs = new TextBox
        {
            Text = existing?.ExtraArgs ?? string.Empty,
            Watermark = L("ExtraArgsWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Tune)
        };

        // Flags
        var chkCredentials = new CheckBox { Content = L("SupportsCredentials"), IsChecked = existing?.SupportsCredentials ?? false };
        var chkWinCredential = new CheckBox
        {
            Content = L("UseWindowsCredential"),
            IsChecked = existing?.UseWindowsCredential ?? false,
            IsVisible = (existing?.Platform ?? LauncherPlatform.Windows) == LauncherPlatform.Windows
        };

        cmbPlatform.SelectionChanged += (_, _) =>
        {
            chkWinCredential.IsVisible = cmbPlatform.SelectedItem is LauncherPlatform.Windows;
        };

        // Error / Save
        var lblError = new TextBlock { Foreground = Brushes.Red, IsVisible = false };

        var btnSave = new Button
        {
            Content = AppIcons.Toolbar(isNew ? AppIcons.Add : AppIcons.Save),
            Padding = new Thickness(6, 4),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };
        Avalonia.Controls.ToolTip.SetTip(btnSave, isNew ? L("Add") : L("Save"));

        var window = new Window
        {
            Title = isNew ? L("NewLauncher") : $"{L("EditLauncher")}: {existing!.DisplayName}",
            Width = 440,
            CanResize = false,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 10,
                Children =
                {
                    new TextBlock { Text = L("LauncherServiceId"),   FontWeight = FontWeight.Bold },
                    txtServiceId,
                    new TextBlock { Text = L("LauncherDisplayName"), FontWeight = FontWeight.Bold },
                    txtDisplayName,
                    new TextBlock { Text = L("LauncherPlatform"), FontWeight = FontWeight.Bold },
                    cmbPlatformWithIcon,
                    new TextBlock { Text = L("LauncherDefaultPort"), FontWeight = FontWeight.Bold },
                    numPortWithIcon,
                    new TextBlock { Text = L("LauncherExecutable"),  FontWeight = FontWeight.Bold },
                    txtExecutable,
                    new TextBlock { Text = L("LauncherArguments"),   FontWeight = FontWeight.Bold },
                    txtArguments,
                    new TextBlock { Text = L("ExtraArgs"),           FontWeight = FontWeight.Bold },
                    txtExtraArgs,
                    chkCredentials,
                    chkWinCredential,
                    lblError,
                    new StackPanel
                    {
                        Orientation         = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing             = 8,
                        Children            = { btnSave }
                    }
                }
            }
        };

        btnSave.Click += (_, _) =>
        {
            var serviceId = txtServiceId.Text?.Trim() ?? string.Empty;
            var displayName = txtDisplayName.Text?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(serviceId) || string.IsNullOrEmpty(displayName))
            {
                lblError.Text = L("LauncherIdAndNameRequired");
                lblError.IsVisible = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(txtExecutable.Text))
            {
                lblError.Text = L("LauncherExecutableRequired");
                lblError.IsVisible = true;
                return;
            }

            window.Close(new LauncherDefinition
            {
                ServiceId = serviceId,
                DisplayName = displayName,
                Platform = (LauncherPlatform)(cmbPlatform.SelectedItem ?? LauncherPlatform.Windows),
                DefaultPort = (int)(numPort.Value ?? 22),
                Executable = txtExecutable.Text!.Trim(),
                Arguments = txtArguments.Text?.Trim() ?? string.Empty,
                ExtraArgs = txtExtraArgs.Text?.Trim() ?? string.Empty,
                SupportsCredentials = chkCredentials.IsChecked == true,
                UseWindowsCredential = chkWinCredential.IsChecked == true,
                DocumentationUrl = existing?.DocumentationUrl ?? string.Empty,
            });
        };

        return await window.ShowDialog<LauncherDefinition?>(owner);
    }
}
