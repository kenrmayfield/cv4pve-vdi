/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Vdi.Config;
using static Corsinvest.ProxmoxVE.Vdi.UI.AppL;
using AGrid = Avalonia.Controls.Grid;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

/// <summary>State and UI controls for the main window.</summary>
internal partial class MainWindowContext(PveClient client, VdiHost host, VdiConfig config)
{
    // ── injected ─────────────────────────────────────────────────────────
    private readonly PveClient _client = client;
    private readonly VdiHost _host = host;
    private readonly VdiConfig _config = config;

    // ── data ─────────────────────────────────────────────────────────────
    private readonly List<ResourceRow> _allRows = [];
    private string _tagColorMap = string.Empty;
    private bool _firstLoad = true;
    private IReadOnlyDictionary<string, IReadOnlyList<string>> _permissions =
        new Dictionary<string, IReadOnlyList<string>>();

    // ── cache ─────────────────────────────────────────────────────────────
    // key=VmId, value=hasSpice — invalidata quando la VM cambia stato running↔stopped
    private readonly Dictionary<long, bool> _spiceConfigCache = [];
    // key=VmId, value=(hasRdp, ip) — invalidata ogni refresh se VM non è running
    private readonly Dictionary<long, (bool HasRdp, string? Ip)> _rdpCache = [];

    // ── filter state ─────────────────────────────────────────────────────
    private string _filterText = string.Empty;
    private readonly HashSet<string> _filterNodes = [];
    private readonly HashSet<string> _filterTags = [];

    // ── topbar labels ────────────────────────────────────────────────────
    private readonly TextBlock _lblRunning = new() { VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
    private readonly TextBlock _lblStopped = new() { VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
    private readonly TextBlock _lblNodes = new() { VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
    private readonly TextBlock _lblVMs = new() { VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
    private readonly TextBlock _lblCTs = new() { VerticalAlignment = VerticalAlignment.Center, FontSize = 12 };
    private readonly TextBlock _lblStatus = new()
    {
        Text = L("Loading"),
        VerticalAlignment = VerticalAlignment.Center,
        FontSize = 11,
        Opacity = 0.6,
        Margin = new Thickness(4, 0, 0, 0)
    };

    // ── content panels ───────────────────────────────────────────────────
    private readonly StackPanel _cardContent = new() { Spacing = 24 };
    private readonly StackPanel _listContent = new() { Spacing = 16, IsVisible = false };

    // ── sidebar controls ─────────────────────────────────────────────────
    private readonly TextBox _txtSearch = new() { Watermark = L("SearchWatermark") };
    private readonly StackPanel _nodeFilters = new() { Spacing = 4 };
    private readonly WrapPanel _tagFilters = new() { Orientation = Orientation.Horizontal };

    private readonly CheckBox _chkRunning = new() { Content = MakeDotLabel(AppColors.Running, L("StatusRunning")), IsChecked = true };
    private readonly CheckBox _chkStopped = new() { Content = MakeDotLabel(AppColors.Stopped, L("StatusStopped")), IsChecked = false };
    private readonly CheckBox _chkQemu = new() { Content = Icons.WithText(Icons.Vm, L("TypeVm")), IsChecked = true };
    private readonly CheckBox _chkLxc = new() { Content = Icons.WithText(Icons.Ct, L("TypeCt")), IsChecked = true };
    private readonly Button _btnReset = new() { Content = Icons.WithText(Icons.Close, L("ResetFilters")), HorizontalAlignment = HorizontalAlignment.Stretch };

    // ── window reference (set in Build) ──────────────────────────────────
    private Window? _window;

    // ── helpers ──────────────────────────────────────────────────────────
    private IBrush ThemeBorderBrush() => AppColors.BorderBrush(_config.Theme == "Dark");

    internal static WindowIcon AppIcon()
    {
        using var stream = Avalonia.Platform.AssetLoader.Open(new Uri("avares://cv4pve-vdi/Corsinvest.ico"));
        return new WindowIcon(stream);
    }

    private static StackPanel MakeDotLabel(Color c, string label) => new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 6,
        Children =
        {
            new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(c), VerticalAlignment = VerticalAlignment.Center },
            new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center }
        }
    };

    public Window Build()
    {
        // ── topbar buttons ────────────────────────────────────────────────
        var btnCardView = new ToggleButton { Content = Icons.Toolbar(Icons.ViewGrid), Padding = new Thickness(6, 4), IsChecked = true };
        var btnListView = new ToggleButton { Content = Icons.Toolbar(Icons.ViewDetail), Padding = new Thickness(6, 4) };
        var btnRefresh = new Button { Content = Icons.Toolbar(Icons.Refresh), Padding = new Thickness(6, 4) };
        var btnAutoRef = new ToggleButton { Content = Icons.Toolbar(Icons.Clock), Padding = new Thickness(6, 4) };
        var btnSettings = new Button { Content = Icons.Toolbar(Icons.Settings), Padding = new Thickness(6, 4) };
        var btnAbout = new Button { Content = Icons.Toolbar(Icons.Info), Padding = new Thickness(6, 4) };

        Avalonia.Controls.ToolTip.SetTip(btnCardView, L("CardView"));
        Avalonia.Controls.ToolTip.SetTip(btnListView, L("DetailView"));
        Avalonia.Controls.ToolTip.SetTip(btnRefresh, L("Refresh"));
        Avalonia.Controls.ToolTip.SetTip(btnAutoRef, L("AutoRefresh"));
        Avalonia.Controls.ToolTip.SetTip(btnSettings, L("Settings"));
        Avalonia.Controls.ToolTip.SetTip(btnAbout, L("About"));

        // ── topbar layout ─────────────────────────────────────────────────
        var statsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 18,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        statsPanel.Children.Add(StatChipDot(_lblRunning, AppColors.Running, L("StatusRunning")));
        statsPanel.Children.Add(StatChipDot(_lblStopped, AppColors.Stopped, L("StatusStopped")));
        statsPanel.Children.Add(StatChipIcon(Icons.Server, _lblNodes, AppColors.StatNodes, L("TypeNode")));
        statsPanel.Children.Add(StatChipIcon(Icons.Vm, _lblVMs, AppColors.StatVMs, L("TypeVm")));
        statsPanel.Children.Add(StatChipIcon(Icons.Ct, _lblCTs, AppColors.StatCTs, L("TypeCt")));

        var logoLbl = new TextBlock
        {
            Text = _host.Name,
            FontWeight = FontWeight.SemiBold,
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center
        };

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        btnPanel.Children.Add(_lblStatus);
        btnPanel.Children.Add(new Separator { Width = 8 });
        btnPanel.Children.Add(btnCardView);
        btnPanel.Children.Add(btnListView);
        btnPanel.Children.Add(new Separator { Width = 8 });
        btnPanel.Children.Add(btnRefresh);
        btnPanel.Children.Add(btnAutoRef);
        btnPanel.Children.Add(btnSettings);
        btnPanel.Children.Add(new Separator { Width = 8 });
        btnPanel.Children.Add(btnAbout);

        var topbarInner = new AGrid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
        };
        AGrid.SetColumn(logoLbl, 0);
        AGrid.SetColumn(statsPanel, 1);
        AGrid.SetColumn(btnPanel, 2);
        topbarInner.Children.Add(logoLbl);
        topbarInner.Children.Add(statsPanel);
        topbarInner.Children.Add(btnPanel);

        var topbar = new Border { Padding = new Thickness(14, 8), Child = topbarInner };

        // ── sidebar ───────────────────────────────────────────────────────
        var sidebar = new ScrollViewer
        {
            Width = 210,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = new StackPanel
            {
                Margin = new Thickness(12, 14),
                Spacing = 0,
                Children =
                {
                    _btnReset,
                    SideSection(L("SectionSearch"), _txtSearch),
                    SideSection(L("SectionNodes"),  new ScrollViewer { MaxHeight = 150, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _nodeFilters }),
                    SideSection(L("SectionStatus"), new StackPanel { Spacing = 4, Children = { _chkRunning, _chkStopped } }),
                    SideSection(L("SectionType"),   new StackPanel { Spacing = 4, Children = { _chkQemu, _chkLxc } }),
                    SideSection(L("SectionTags"),   new ScrollViewer { MaxHeight = 150, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = _tagFilters }),
                }
            }
        };

        // ── main grid ─────────────────────────────────────────────────────
        var scrollContent = new ScrollViewer
        {
            Content = new StackPanel { Margin = new Thickness(18, 14), Spacing = 0, Children = { _cardContent, _listContent } },
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var mainGrid = new AGrid
        {
            RowDefinitions = new RowDefinitions("Auto,*"),
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        AGrid.SetRow(topbar, 0); AGrid.SetColumnSpan(topbar, 2);
        AGrid.SetRow(scrollContent, 1); AGrid.SetColumn(scrollContent, 0);
        AGrid.SetRow(sidebar, 1); AGrid.SetColumn(sidebar, 1);
        mainGrid.Children.Add(topbar);
        mainGrid.Children.Add(scrollContent);
        mainGrid.Children.Add(sidebar);

        // ── busy overlay ──────────────────────────────────────────────────
        var busyOverlay = new Border
        {
            IsVisible = false,
            Background = new SolidColorBrush(AppColors.BusyOverlay),
            Child = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 12,
                Children =
                {
                    new ProgressBar { IsIndeterminate = true, Width = 220 },
                    new TextBlock {
                        Text = L("Loading"),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = Brushes.White,
                        FontSize = 14
                    }
                }
            }
        };

        _window = new Window
        {
            Title = "cv4pve-vdi",
            Width = 1200,
            Height = 700,
            MinWidth = 800,
            MinHeight = 500,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
            Content = new Panel { Children = { mainGrid, busyOverlay } }
        };

        // ── view toggle ───────────────────────────────────────────────────
        btnCardView.IsCheckedChanged += (_, _) =>
        {
            if (btnCardView.IsChecked == true)
            {
                btnListView.IsChecked = false;
                _cardContent.IsVisible = true;
                _listContent.IsVisible = false;
            }
        };
        btnListView.IsCheckedChanged += (_, _) =>
        {
            if (btnListView.IsChecked == true)
            {
                btnCardView.IsChecked = false;
                _cardContent.IsVisible = false;
                _listContent.IsVisible = true;
            }
        };

        // ── sidebar wiring ────────────────────────────────────────────────
        _txtSearch.TextChanged += (_, _) => { _filterText = _txtSearch.Text ?? ""; ApplyFilter(); };
        _chkRunning.IsCheckedChanged += (_, _) => ApplyFilter();
        _chkStopped.IsCheckedChanged += (_, _) => ApplyFilter();
        _chkQemu.IsCheckedChanged += (_, _) => ApplyFilter();
        _chkLxc.IsCheckedChanged += (_, _) => ApplyFilter();

        _btnReset.Click += (_, _) =>
        {
            _txtSearch.Text = "";
            _chkRunning.IsChecked = _chkStopped.IsChecked = _chkQemu.IsChecked = _chkLxc.IsChecked = true;
            _filterNodes.Clear();
            foreach (var child in _nodeFilters.Children.OfType<CheckBox>())
            {
                child.IsChecked = true;
            }

            _filterTags.Clear();
            foreach (var child in _tagFilters.Children.OfType<ToggleButton>())
            {
                child.IsChecked = false;
            }

            ApplyFilter();
        };

        // ── settings ──────────────────────────────────────────────────────
        btnSettings.Click += async (_, _) =>
        {
            var w = SettingsWindow.Create(_config);
            w.Icon = MainWindowContext.AppIcon();
            await w.ShowDialog(_window);
        };

        // ── about ─────────────────────────────────────────────────────────
        btnAbout.Click += async (_, _) => await ShowAboutAsync();

        // ── refresh ───────────────────────────────────────────────────────
        btnRefresh.Click += async (_, _) => await RefreshAsync(busyOverlay);

        CancellationTokenSource? autoRefreshCts = null;
        btnAutoRef.IsCheckedChanged += (_, _) =>
        {
            if (btnAutoRef.IsChecked == true)
            {
                autoRefreshCts = new CancellationTokenSource();
                var token = autoRefreshCts.Token;
                Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Task.Delay(10_000, token).ContinueWith(_ => { });
                        if (!token.IsCancellationRequested)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() => RefreshAsync(busyOverlay));
                        }
                    }
                }, token);
            }
            else { autoRefreshCts?.Cancel(); autoRefreshCts = null; }
        };

        _window.Closing += (_, _) => autoRefreshCts?.Cancel();
        _window.Opened += async (_, _) => await RefreshAsync(busyOverlay);

        return _window;
    }

    // ── topbar chip helpers ───────────────────────────────────────────────
    private static Control StatChipDot(TextBlock valueLabel, Color dotColor, string label)
        => new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 4),
            Background = new SolidColorBrush(Color.FromArgb(20, dotColor.R, dotColor.G, dotColor.B)),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new Ellipse { Width = 6, Height = 6, Fill = new SolidColorBrush(dotColor), VerticalAlignment = VerticalAlignment.Center },
                    valueLabel,
                    new TextBlock { Text = label, FontSize = 11, Opacity = 0.55, VerticalAlignment = VerticalAlignment.Center }
                }
            }
        };

    private static Control StatChipIcon(string iconData, TextBlock valueLabel, Color iconColor, string label)
        => new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 4),
            Background = new SolidColorBrush(Color.FromArgb(20, iconColor.R, iconColor.G, iconColor.B)),
            Child = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                VerticalAlignment = VerticalAlignment.Center,
                Children =
                {
                    new PathIcon { Data = Geometry.Parse(iconData), Width = 13, Height = 13, Foreground = new SolidColorBrush(iconColor) },
                    valueLabel,
                    new TextBlock { Text = label, FontSize = 11, Opacity = 0.55, VerticalAlignment = VerticalAlignment.Center }
                }
            }
        };

    // ── sidebar section helper ────────────────────────────────────────────
    private static Border SideSection(string title, Control content)
        => new()
        {
            Padding = new Thickness(0, 0, 0, 14),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock {
                        Text = title,
                        FontSize = 10,
                        FontWeight = FontWeight.Bold,
                        Opacity = 0.5,
                        Margin = new Thickness(0, 0, 0, 2)
                    },
                    content
                }
            }
        };
}
