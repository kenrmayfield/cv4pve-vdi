/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static partial class SettingsWindow
{
    private static (TabItem Tab, Action Save) BuildTabAppearance(AppConfig config)
    {
        var themes = new[] { AppConfig.ThemeSystem, AppConfig.ThemeLight, AppConfig.ThemeDark };

        var cmbTheme = new ComboBox
        {
            ItemsSource = themes,
            SelectedItem = themes.Contains(config.Theme) ? config.Theme : AppConfig.ThemeSystem,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 120,
            Padding = new Thickness(26, 0, 0, 0)
        };

        var cmbThemeWithIcon = new Grid();
        cmbThemeWithIcon.Children.Add(cmbTheme);
        cmbThemeWithIcon.Children.Add(AppIcons.InnerOverlay(AppIcons.Palette));

        var isCard = config.DefaultView != AppConfig.ViewList;
        var btnViewCard = new ToggleButton
        {
            Content = AppIcons.WithText(AppIcons.ViewGrid, AppConfig.ViewCard),
            IsChecked = isCard,
            Padding = new Thickness(8, 4),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent
        };
        var btnViewList = new ToggleButton
        {
            Content = AppIcons.WithText(AppIcons.ViewDetail, AppConfig.ViewList),
            IsChecked = !isCard,
            Padding = new Thickness(8, 4),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent
        };
        btnViewCard.IsCheckedChanged += (_, _) => { if (btnViewCard.IsChecked == true) { btnViewList.IsChecked = false; } };
        btnViewList.IsCheckedChanged += (_, _) => { if (btnViewList.IsChecked == true) { btnViewCard.IsChecked = false; } };

        var viewToggle = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        viewToggle.Children.Add(btnViewCard);
        viewToggle.Children.Add(btnViewList);

        var themeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock { Text = L("Theme"), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center },
                cmbThemeWithIcon,
                new TextBlock { Text = L("DefaultView"), FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) },
                viewToggle
            }
        };

        var chkShowBars = new CheckBox { Content = AppIcons.WithText(AppIcons.ChartBar, L("ShowBars")), IsChecked = config.ShowBars };
        var chkShowNodes = new CheckBox { Content = AppIcons.WithText(AppIcons.Server, L("ShowNodes")), IsChecked = config.ShowNodes };
        var chkShowPools = new CheckBox { Content = AppIcons.WithText(AppIcons.Folder, L("ShowPools")), IsChecked = config.ShowPools };
        var chkShowTags = new CheckBox { Content = AppIcons.WithText(AppIcons.Tag, L("ShowTags")), IsChecked = config.ShowTags };

        var chkEnableAgentPing = new CheckBox { Content = AppIcons.WithText(AppIcons.Server, L("EnableAgentPing")), IsChecked = config.EnableAgentPing };

        var chkShowStart = new CheckBox { Content = AppIcons.WithText(AppIcons.Play, L("ShowStartButton"), new SolidColorBrush(AppColors.Running)), IsChecked = config.ShowStartButton };
        var chkConfirmStart = new CheckBox { Content = L("AskConfirmation"), IsChecked = config.ConfirmStart, IsEnabled = config.ShowStartButton, HorizontalAlignment = HorizontalAlignment.Right };
        var chkShowShutdown = new CheckBox { Content = AppIcons.WithText(AppIcons.Stop, L("ShowShutdownButton"), new SolidColorBrush(AppColors.Shutdown)), IsChecked = config.ShowShutdownButton };
        var chkConfirmShutdown = new CheckBox { Content = L("AskConfirmation"), IsChecked = config.ConfirmShutdown, IsEnabled = config.ShowShutdownButton, HorizontalAlignment = HorizontalAlignment.Right };

        Avalonia.Controls.Grid.SetColumn(chkConfirmStart, 1);
        Avalonia.Controls.Grid.SetColumn(chkConfirmShutdown, 1);

        chkShowStart.IsCheckedChanged += (_, _) => chkConfirmStart.IsEnabled = chkShowStart.IsChecked == true;
        chkShowShutdown.IsCheckedChanged += (_, _) => chkConfirmShutdown.IsEnabled = chkShowShutdown.IsChecked == true;

        var tab = new TabItem
        {
            Header = AppIcons.WithText(AppIcons.Palette, L("TabAppearance")),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8,
                Children =
                {
                    SectionHeader(L("SectionDisplay")),
                    themeRow,
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("*,*"),
                        RowDefinitions    = new RowDefinitions("Auto,Auto"),
                        Children =
                        {
                            chkShowBars,
                            Placed(chkShowNodes, 1, 0),
                            Placed(chkShowPools, 0, 1),
                            Placed(chkShowTags,  1, 1),
                        }
                    },
                    SectionHeader(L("SectionBehaviour")),
                    chkEnableAgentPing,
                    SectionHeader(L("PowerButtons")),
                    new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { chkShowStart,    chkConfirmStart } },
                    new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Children = { chkShowShutdown, chkConfirmShutdown } },
                }
            }
        };

        void Save()
        {
            config.Theme = cmbTheme.SelectedItem as string ?? AppConfig.ThemeSystem;
            config.DefaultView = btnViewList.IsChecked == true ? AppConfig.ViewList : AppConfig.ViewCard;
            config.ShowBars = chkShowBars.IsChecked == true;
            config.ShowNodes = chkShowNodes.IsChecked == true;
            config.ShowPools = chkShowPools.IsChecked == true;
            config.ShowTags = chkShowTags.IsChecked == true;
            config.ShowStartButton = chkShowStart.IsChecked == true;
            config.ConfirmStart = chkConfirmStart.IsChecked == true;
            config.ShowShutdownButton = chkShowShutdown.IsChecked == true;
            config.ConfirmShutdown = chkConfirmShutdown.IsChecked == true;
            config.EnableAgentPing = chkEnableAgentPing.IsChecked == true;
        }

        return (tab, Save);
    }

    private static Control Placed(Control c, int col, int row)
    {
        Avalonia.Controls.Grid.SetColumn(c, col);
        Avalonia.Controls.Grid.SetRow(c, row);
        return c;
    }
}
