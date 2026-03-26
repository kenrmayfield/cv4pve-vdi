/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static partial class SettingsWindow
{
    public static Window Create(AppConfig config, Action? onHostsChanged = null, int initialTab = 0, bool clustersOnly = false)
    {
        var btnSave = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Save),
            Padding = new Thickness(6, 4),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };
        Avalonia.Controls.ToolTip.SetTip(btnSave, L("Save"));

        var btnRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(16, 8),
            Spacing = 8
        };
        btnRow.Children.Add(btnSave);

        var dock = new DockPanel();
        Avalonia.Controls.DockPanel.SetDock(btnRow, Dock.Bottom);
        dock.Children.Add(btnRow);

        var tabControl = new TabControl { Margin = new Thickness(12, 12, 12, 0) };

        var window = new Window
        {
            Title = L("SettingsWindowTitle"),
            MinWidth = 500,
            MinHeight = 300,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = dock
        };

        var (tabAppearance, saveAppearance) = BuildTabAppearance(config);
        var (tabLaunchers, saveLaunchers) = BuildTabLaunchers(config, window);
        var (tabClusters, _) = BuildTabClusters(config, window, onHostsChanged);

        if (clustersOnly)
        {
            tabControl.Items.Add(tabClusters);
        }
        else
        {
            tabControl.Items.Add(tabAppearance);
            tabControl.Items.Add(tabLaunchers);
            tabControl.Items.Add(tabClusters);
            tabControl.SelectedIndex = initialTab;
        }

        dock.Children.Add(tabControl);

        btnSave.Click += (_, _) =>
        {
            saveAppearance();
            saveLaunchers();
            AppConfigManager.Save(config);
            Avalonia.Application.Current?.RequestedThemeVariant = config.ThemeVariant;
            window.Close();
        };

        return window;
    }

    // Section header helper shared across partial files
    internal static StackPanel SectionHeader(string label) => new()
    {
        Spacing = 4,
        Margin = new Thickness(0, 4, 0, 0),
        Children =
        {
            new TextBlock
            {
                Text = label,
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Color.FromArgb(160, 128, 128, 128)),
                LetterSpacing = 1.2
            },
            new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128))
            }
        }
    };
}
