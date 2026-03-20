/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static class SettingsWindow
{
    public static Window Create(VdiConfig config, Action? onHostsChanged = null, int initialTab = 0, bool clustersOnly = false)
    {
        // Tab 1: Appearance
        var themes = new[] { VdiConfig.ThemeSystem, VdiConfig.ThemeLight, VdiConfig.ThemeDark };

        var cmbTheme = new ComboBox
        {
            ItemsSource = themes,
            SelectedItem = themes.Contains(config.Theme)
                            ? config.Theme
                            : VdiConfig.ThemeSystem,

            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(26, 0, 0, 0)
        };

        var cmbThemeWithIcon = new Grid();
        cmbThemeWithIcon.Children.Add(cmbTheme);
        cmbThemeWithIcon.Children.Add(AppIcons.InnerOverlay(AppIcons.Palette));

        var chkShowBars = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.ChartBar, L("ShowBars")),
            IsChecked = config.ShowBars
        };

        var chkShowStart = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.Play, L("ShowStartButton"), new SolidColorBrush(AppColors.Running)),
            IsChecked = config.ShowStartButton
        };

        var chkConfirmStart = new CheckBox
        {
            Content = L("AskConfirmation"),
            IsChecked = config.ConfirmStart,
            IsEnabled = config.ShowStartButton,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var chkShowShutdown = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.Stop, L("ShowShutdownButton"), new SolidColorBrush(AppColors.Shutdown)),
            IsChecked = config.ShowShutdownButton
        };

        var chkConfirmShutdown = new CheckBox
        {
            Content = L("AskConfirmation"),
            IsChecked = config.ConfirmShutdown,
            IsEnabled = config.ShowShutdownButton,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        Avalonia.Controls.Grid.SetColumn(chkConfirmStart, 1);
        Avalonia.Controls.Grid.SetColumn(chkConfirmShutdown, 1);

        chkShowStart.IsCheckedChanged += (_, _) => chkConfirmStart.IsEnabled = chkShowStart.IsChecked == true;
        chkShowShutdown.IsCheckedChanged += (_, _) => chkConfirmShutdown.IsEnabled = chkShowShutdown.IsChecked == true;

        var chkShowNodes = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.Server, L("ShowNodes")),
            IsChecked = config.ShowNodes
        };

        var chkShowPools = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.Folder, L("ShowPools")),
            IsChecked = config.ShowPools
        };

        var chkShowTags = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.Tag, L("ShowTags")),
            IsChecked = config.ShowTags
        };

        // View toggle (Card / List)
        var isCard = config.DefaultView != VdiConfig.ViewList;
        var btnViewCard = new ToggleButton
        {
            Content = AppIcons.WithText(AppIcons.ViewGrid, VdiConfig.ViewCard),
            IsChecked = isCard,
            Padding = new Thickness(8, 4),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent
        };
        var btnViewList = new ToggleButton
        {
            Content = AppIcons.WithText(AppIcons.ViewDetail, VdiConfig.ViewList),
            IsChecked = !isCard,
            Padding = new Thickness(8, 4),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent
        };
        btnViewCard.IsCheckedChanged += (_, _) => { if (btnViewCard.IsChecked == true) { btnViewList.IsChecked = false; } };
        btnViewList.IsCheckedChanged += (_, _) => { if (btnViewList.IsChecked == true) { btnViewCard.IsChecked = false; } };
        var viewToggle = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2
        };
        viewToggle.Children.Add(btnViewCard);
        viewToggle.Children.Add(btnViewList);

        // Theme row: label + fixed-width combo + view label + view combo
        cmbTheme.HorizontalAlignment = HorizontalAlignment.Left;
        cmbTheme.Width = 120;
        var themeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = L("Theme"),
                    FontWeight = FontWeight.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                },
                cmbThemeWithIcon,
                new TextBlock
                {
                    Text = L("DefaultView"),
                    FontWeight = FontWeight.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                },
                viewToggle
            }
        };

        // Grid placement helper
        static Control Placed(Control c, int col, int row)
        {
            Avalonia.Controls.Grid.SetColumn(c, col);
            Avalonia.Controls.Grid.SetRow(c, row);
            return c;
        }

        // Section helper
        static StackPanel SectionHeader(string label) => new()
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

        var tabAppearance = new TabItem
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
                        RowDefinitions = new RowDefinitions("Auto,Auto"),
                        Children =
                        {
                            chkShowBars,
                            Placed(chkShowNodes, 1, 0),
                            Placed(chkShowPools, 0, 1),
                            Placed(chkShowTags, 1, 1),
                        }
                    },
                    SectionHeader(L("PowerButtons")),
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                        Children = { chkShowStart, chkConfirmStart }
                    },
                    new Grid
                    {
                        ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                        Children = { chkShowShutdown, chkConfirmShutdown }
                    },
                }
            }
        };

        // Tab 2: Viewer ─
        var txtViewerPath = new TextBox
        {
            Text = config.ViewerPath,
            Watermark = L("SpiceViewerWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Spice)
        };

        var btnBrowseViewer = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Folder),
            Padding = new Thickness(6, 4),
            Margin = new Thickness(4, 0, 0, 0)
        };

        var viewerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        viewerRow.Add(txtViewerPath, 0);
        viewerRow.Add(btnBrowseViewer, 1);

        var txtRdpPath = new TextBox
        {
            Text = config.RdpPath,
            Watermark = L("RdpClientWatermark"),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            InnerLeftContent = AppIcons.Inner(AppIcons.Rdp)
        };

        var btnBrowseRdp = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Folder),
            Padding = new Thickness(6, 4),
            Margin = new Thickness(4, 0, 0, 0)
        };

        var rdpRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        rdpRow.Add(txtRdpPath, 0);
        rdpRow.Add(btnBrowseRdp, 1);

        var chkSpice = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.Spice, "SPICE"),
            IsChecked = config.EnableSpice
        };

        var chkVnc = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.Vnc, "VNC"),
            IsChecked = config.EnableVnc
        };

        var chkRdp = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.Rdp, "RDP"),
            IsChecked = config.EnableRdp
        };

        var chkAgentPing = new CheckBox
        {
            Content = AppIcons.WithText(AppIcons.Agent, L("AgentPing")),
            IsChecked = config.EnableAgentPing
        };

        var tabConnectivity = new TabItem
        {
            Header = AppIcons.WithText(AppIcons.Spice, L("TabViewer")),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8,
                Children =
                {
                    SectionHeader(L("HostProtocols")),
                    new WrapPanel
                    {
                        Orientation = Orientation.Horizontal,
                        ItemWidth = 160,
                        Children = { chkSpice, chkVnc, chkRdp, chkAgentPing }
                    },
                    SectionHeader(L("SpiceViewerPath")),
                    new HyperlinkButton
                    {
                        Content = "Download remote-viewer (spice-space.org)",
                        NavigateUri = new Uri("https://www.spice-space.org/download.html"),
                        FontSize = 11,
                        Padding = new Thickness(0)
                    },
                    viewerRow,
                    SectionHeader(L("RdpClientPath")),
                    rdpRow,
                }
            }
        };

        // Tab 3: Clusters
        var hostItems = new ObservableCollection<VdiHost>(config.Hosts);

        var lstHosts = new ListBox
        {
            ItemsSource = hostItems,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Height = 160,
            ItemTemplate = new FuncDataTemplate<VdiHost>((h, _) =>
                new TextBlock
                {
                    Text = h == null
                            ? string.Empty
                            : $"{h.Name}  ({h.Hosts})"
                })
        };

        var btnAddHost = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Add),
            Padding = new Thickness(6, 4),
            Margin = new Thickness(0, 0, 4, 0)
        };
        Avalonia.Controls.ToolTip.SetTip(btnAddHost, L("Add"));

        var btnEditHost = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Edit),
            Padding = new Thickness(6, 4),
            Margin = new Thickness(0, 0, 4, 0),
            IsEnabled = false
        };
        Avalonia.Controls.ToolTip.SetTip(btnEditHost, L("Edit"));

        var btnDelHost = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Delete),
            Padding = new Thickness(6, 4),
            IsEnabled = false
        };
        Avalonia.Controls.ToolTip.SetTip(btnDelHost, L("Delete"));

        lstHosts.SelectionChanged += (_, _) =>
        {
            btnEditHost.IsEnabled = lstHosts.SelectedIndex >= 0;
            btnDelHost.IsEnabled = lstHosts.SelectedIndex >= 0;
        };

        var tabClusters = new TabItem
        {
            Header = AppIcons.WithText(AppIcons.Server, L("TabClusters")),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8,
                Children =
                {
                    lstHosts,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Children    = { btnAddHost, btnEditHost, btnDelHost }
                    }
                }
            }
        };

        var btnSave = new Button
        {
            Content = AppIcons.Toolbar(AppIcons.Save),
            Padding = new Thickness(6, 4)
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
        var tabControl = new TabControl
        {
            Margin = new Thickness(12, 12, 12, 0),
        };

        if (clustersOnly)
        {
            tabControl.Items.Add(tabClusters);
        }
        else
        {
            tabControl.Items.Add(tabAppearance);
            tabControl.Items.Add(tabConnectivity);
            tabControl.Items.Add(tabClusters);
            tabControl.SelectedIndex = initialTab;
        }
        dock.Children.Add(tabControl);

        var window = new Window
        {
            Title = L("SettingsWindowTitle"),
            Width = 500,
            MinHeight = 300,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = dock
        };

        async Task BrowseFile(TextBox target, string titleKey)
        {
            var topLevel = TopLevel.GetTopLevel(window);
            if (topLevel == null) { return; }

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new()
            {
                Title = L(titleKey),
                AllowMultiple = false
            });

            if (files.Count > 0) { target.Text = files[0].Path.LocalPath; }
        }

        btnBrowseViewer.Click += async (_, _) => await BrowseFile(txtViewerPath, "SelectSpiceViewer");
        btnBrowseRdp.Click += async (_, _) => await BrowseFile(txtRdpPath, "SelectRdpClient");

        async Task EditHostAt(int idx)
        {
            if (idx < 0 || idx >= config.Hosts.Count) { return; }

            var dlg = HostEditWindow.Create(config.Hosts[idx]);
            dlg.Icon = MainWindow.AppIcon();
            var result = await dlg.ShowDialog<VdiHost?>(window);
            if (result != null)
            {
                config.Hosts[idx] = result;
                hostItems[idx] = result;
                VdiConfigManager.Save(config);
                onHostsChanged?.Invoke();
                lstHosts.SelectedIndex = idx;
            }
        }

        lstHosts.DoubleTapped += async (_, _) => await EditHostAt(lstHosts.SelectedIndex);
        btnEditHost.Click += async (_, _) => await EditHostAt(lstHosts.SelectedIndex);

        btnAddHost.Click += async (_, _) =>
        {
            var dlg = HostEditWindow.Create(null);
            dlg.Icon = MainWindow.AppIcon();
            var result = await dlg.ShowDialog<VdiHost?>(window);
            if (result != null)
            {
                config.Hosts.Add(result);
                hostItems.Add(result);
                VdiConfigManager.Save(config);
                onHostsChanged?.Invoke();
                lstHosts.SelectedIndex = hostItems.Count - 1;
            }
        };

        btnDelHost.Click += (_, _) =>
        {
            if (lstHosts.SelectedIndex < 0 || lstHosts.SelectedIndex >= config.Hosts.Count) { return; }

            config.Hosts.RemoveAt(lstHosts.SelectedIndex);
            hostItems.RemoveAt(lstHosts.SelectedIndex);
            VdiConfigManager.Save(config);
            onHostsChanged?.Invoke();
        };

        btnSave.Click += (_, _) =>
        {
            config.ViewerPath = txtViewerPath.Text ?? string.Empty;
            config.RdpPath = txtRdpPath.Text ?? string.Empty;
            config.Theme = cmbTheme.SelectedItem as string ?? VdiConfig.ThemeSystem;
            config.ShowBars = chkShowBars.IsChecked == true;
            config.ShowStartButton = chkShowStart.IsChecked == true;
            config.ShowShutdownButton = chkShowShutdown.IsChecked == true;
            config.ConfirmStart = chkConfirmStart.IsChecked == true;
            config.ConfirmShutdown = chkConfirmShutdown.IsChecked == true;
            config.EnableSpice = chkSpice.IsChecked == true;
            config.EnableVnc = chkVnc.IsChecked == true;
            config.EnableRdp = chkRdp.IsChecked == true;
            config.EnableAgentPing = chkAgentPing.IsChecked == true;
            config.ShowNodes = chkShowNodes.IsChecked == true;
            config.ShowPools = chkShowPools.IsChecked == true;
            config.ShowTags = chkShowTags.IsChecked == true;
            config.DefaultView = btnViewList.IsChecked == true ? VdiConfig.ViewList : VdiConfig.ViewCard;
            VdiConfigManager.Save(config);

            Avalonia.Application.Current?.RequestedThemeVariant = config.ThemeVariant;
            window.Close();
        };

        return window;
    }
}
