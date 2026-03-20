/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;
using Corsinvest.ProxmoxVE.Vdi.UI.Models;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    private static void OpenUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* ignore */ }
    }

    private static IBrush ThemeBorderBrush() => AppColors.BorderBrush(AppColors.IsDark);

    internal void BuildAgentBadge(ResourceRow row, StackPanel parent)
    {
        if (!row.Features.AgentConfigured)
        {
            return;
        }

        string tooltip;
        Color color;
        double opacity;

        if (!_config.EnableAgentPing)
        {
            tooltip = L("BadgeAgentUnknown");
            color = Colors.Gray;
            opacity = 0.5;
        }
        else if (row.Features.AgentRunning)
        {
            tooltip = L("BadgeAgentRunning");
            color = AppColors.Running;
            opacity = 0.9;
        }
        else
        {
            tooltip = L("BadgeAgentStopped");
            color = AppColors.Shutdown;
            opacity = 0.9;
        }

        var icon = new PathIcon
        {
            Data = Geometry.Parse(AppIcons.Agent),
            Width = 11,
            Height = 11,
            Foreground = new SolidColorBrush(color),
            Opacity = opacity,
            VerticalAlignment = VerticalAlignment.Center
        };
        Avalonia.Controls.ToolTip.SetTip(icon, tooltip);
        parent.Children.Add(icon);
    }

    internal static Control BuildFeatureBadges(ResourceRow row)
    {
        if (!row.Features.Audio && !row.Features.UsbRedirect && !row.Features.Clipboard)
        {
            return new Border();
        }

        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };

        void AddBadge(string icon, string tooltip, Color color, double opacity = 0.9)
        {
            var pathIcon = new PathIcon
            {
                Data = Geometry.Parse(icon),
                Width = 12,
                Height = 12,
                Foreground = new SolidColorBrush(color),
                Opacity = opacity
            };
            Avalonia.Controls.ToolTip.SetTip(pathIcon, tooltip);
            panel.Children.Add(pathIcon);
        }

        if (row.Features.Audio) { AddBadge(AppIcons.Audio, L("BadgeAudioSpice"), AppColors.Running); }
        if (row.Features.UsbRedirect) { AddBadge(AppIcons.Usb, L("BadgeUsbRedirect"), AppColors.Running); }
        if (row.Features.Clipboard) { AddBadge(AppIcons.Clipboard2, L("BadgeClipboard"), AppColors.Running); }

        return panel;
    }

    private static Button ActionButton(string icon, string tooltip, Thickness padding, Color? foreground = null)
    {
        var btn = new Button
        {
            Content = AppIcons.Row(icon, foreground.HasValue ? new SolidColorBrush(foreground.Value) : null),
            Padding = padding,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0)
        };
        Avalonia.Controls.ToolTip.SetTip(btn, tooltip);
        return btn;
    }

    internal void AddActionButtons(StackPanel panel, ResourceRow row, bool isCard)
    {
        var padding = isCard
                        ? new Thickness(8, 6)
                        : new Thickness(4, 2);

        if (_config.ShowStartButton && row.CanPower && !row.Resource.IsRunning)
        {
            var btn = ActionButton(AppIcons.Play, L("Start"), padding, AppColors.Running);
            btn.Click += async (_, _) =>
            {
                if (_config.ConfirmStart && !await ConfirmAsync(string.Format(L("ConfirmStart"), row.Name)))
                {
                    return;
                }

                await VmService.ChangeStatusAsync(_client, row.Resource.Node, row.Resource.VmId, row.VmType, VmStatus.Start);
                if (_btnAutoRef != null && _btnAutoRef.IsChecked != true)
                {
                    _btnAutoRef.IsChecked = true;
                }
                await RefreshAsync();
            };
            panel.Children.Add(btn);
        }

        if (_config.ShowShutdownButton && row.CanPower && row.Resource.IsRunning)
        {
            var btn = ActionButton(AppIcons.Stop, L("Shutdown"), padding, AppColors.Shutdown);
            btn.Click += async (_, _) =>
            {
                if (_config.ConfirmShutdown && !await ConfirmAsync(string.Format(L("ConfirmShutdown"), row.Name)))
                {
                    return;
                }

                await VmService.ChangeStatusAsync(_client, row.Resource.Node, row.Resource.VmId, row.VmType, VmStatus.Shutdown);
                if (_btnAutoRef != null && _btnAutoRef.IsChecked != true)
                {
                    _btnAutoRef.IsChecked = true;
                }
                await RefreshAsync();
            };
            panel.Children.Add(btn);
        }

        if (row.CanSpice)
        {
            var btn = ActionButton(AppIcons.Spice, L("Spice"), padding);
            btn.Click += async (_, _) => await LaunchSpiceAsync(row);
            panel.Children.Add(btn);
        }

        if (_config.EnableVnc && row.CanVnc)
        {
            var btn = ActionButton(AppIcons.Vnc, L("Vnc"), padding);
            btn.Click += async (_, _) => await LaunchVncAsync(row);
            panel.Children.Add(btn);
        }

        if (row.HasRdp && row.RdpIp is not null)
        {
            var btn = ActionButton(AppIcons.Rdp, string.Format(L("StatusRdpTooltip"), row.RdpIp), padding);
            btn.Click += (_, _) =>
            {
                var err = VmService.LaunchRdp(row.RdpIp, _config.RdpPath);
                if (!string.IsNullOrEmpty(err))
                {
                    ShowToast($"{L("ErrorPrefix")}{err}", NotificationSeverity.Error);
                }
            };
            panel.Children.Add(btn);
        }
    }

    private Color GetTagColor(string tag)
    {
        foreach (var entry in _tagColorMap.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = entry.Split(':');
            if (parts.Length >= 3 && string.Equals(parts[0], tag, StringComparison.OrdinalIgnoreCase))
            {
                var hex = parts[2].TrimStart('#');
                try { return Color.Parse("#" + hex); } catch { }
            }
        }
        var h = Math.Abs(tag.GetHashCode());
        return Color.FromRgb((byte)(h & 0xFF), (byte)((h >> 8) & 0xFF), (byte)((h >> 16) & 0x7F | 0x40));
    }

    private static Ellipse BuildStatusDot(ResourceRow row)
        => new()
        {
            Width = 8,
            Height = 8,
            Fill = new SolidColorBrush(row.IsActive
                    ? AppColors.Running
                    : AppColors.Stopped),
            VerticalAlignment = VerticalAlignment.Center
        };

    private static Border BuildTypeBadge(ResourceRow row)
    {
        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 3,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new PathIcon
                {
                    Data = Geometry.Parse(row.ResourceType == ClusterResourceType.Node
                                            ? AppIcons.Server
                                            : row.VmType == VmType.Qemu
                                                ? AppIcons.Vm
                                                : AppIcons.Ct),
                    Width = 12,
                    Height = 12,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = row.ResourceType == ClusterResourceType.Node
                            ? L("TypeNode")
                            : row.VmType == VmType.Qemu
                                ? L("TypeVm")
                                : L("TypeCt"),
                    FontSize = 11,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };

        if (row.ResourceType != ClusterResourceType.Node && !string.IsNullOrEmpty(row.OsType))
        {
            stack.Children.Add(new PathIcon
            {
                Data = Geometry.Parse(row.OsType.StartsWith("win")
                        ? AppIcons.Windows
                        : AppIcons.Linux),
                Width = 12,
                Height = 12,
                Foreground = new SolidColorBrush(AppColors.OsBrushColor(row.OsType)),
                Margin = new Thickness(2, 0, 0, 0)
            });
        }

        return new Border
        {
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(5, 1),
            Background = new SolidColorBrush(AppColors.TypeBadge),
            Child = stack
        };
    }

    private WrapPanel BuildTagsPanel(IEnumerable<string> tags)
    {
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (var tag in tags)
        {
            panel.Children.Add(BuildTagBadge(tag));
        }

        return panel;
    }

    private Border BuildTagBadge(string tag)
    {
        var color = GetTagColor(tag);
        // dark: blend toward white; light: blend toward black
        static Color Blend(Color c, Color target, double t) => Color.FromArgb(255,
            (byte)(c.R + (target.R - c.R) * t),
            (byte)(c.G + (target.G - c.G) * t),
            (byte)(c.B + (target.B - c.B) * t));
        var textColor = AppColors.IsDark
            ? Blend(color, Colors.White, 0.6)
            : Blend(color, Colors.Black, 0.5);
        return new Border
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(7, 2),
            Margin = new Thickness(0, 0, 4, 2),
            Background = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(180, textColor.R, textColor.G, textColor.B)),
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = tag,
                FontSize = 10,
                Foreground = new SolidColorBrush(textColor),
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private StackPanel BuildNodeHeader(string nodeName, ResourceRow? nodeRow)
    {
        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        header.Children.Add(new TextBlock
        {
            Text = nodeName,
            FontWeight = FontWeight.SemiBold,
            FontSize = 13
        }.Secondary());

        if (nodeRow != null)
        {
            header.Children.Add(new TextBlock
            {
                Text = $"{nodeRow.CpuDisplay}  {nodeRow.MemoryDisplay}",
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            }.Secondary());
        }
        return header;
    }

    private static Border BuildSectionDivider(double opacity = 0.12)
        => new()
        {
            Height = 1,
            Opacity = opacity,
            Margin = new Thickness(0, 2, 0, 0)
        };

    // Iterates node+vm groups and calls the callback to build each group's content
    private void ForEachNodeGroup(List<ResourceRow> rows, Action<string, ResourceRow?, List<ResourceRow>> callback)
    {
        foreach (var nodeName in GetOrderedNodeNames(rows))
        {
            var nodeRow = _allRows.FirstOrDefault(r => r.ResourceType == ClusterResourceType.Node && r.Name == nodeName);
            var nodeVms = rows.Where(r => r.ResourceType == ClusterResourceType.Vm && r.NodeName == nodeName).ToList();
            if (nodeRow == null && nodeVms.Count == 0)
            {
                continue;
            }
            callback(nodeName, nodeRow, nodeVms);
        }
    }

    private static List<string> GetOrderedNodeNames(List<ResourceRow> rows)
        => rows.Where(r => r.ResourceType == ClusterResourceType.Vm)
               .Select(r => r.NodeName)
               .Distinct()
               .Union(rows.Where(r => r.ResourceType == ClusterResourceType.Node).Select(r => r.Name))
               .OrderBy(n => n)
               .ToList();

    internal IEnumerable<string> EffectivePrivs(string path)
    {
        if (_permissions.TryGetValue("/", out var rootPrivs))
        {
            foreach (var p in rootPrivs)
            {
                yield return p;
            }
        }

        var current = string.Empty;
        foreach (var part in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            current += "/" + part;
            if (_permissions.TryGetValue(current, out var privs))
            {
                foreach (var p in privs)
                {
                    yield return p;
                }
            }
        }
    }

    internal void UpdateStats(int nodeCount)
    {
        _lblRunning.Text = _allRows.Count(r => r.IsActive && r.ResourceType == ClusterResourceType.Vm).ToString();
        _lblStopped.Text = _allRows.Count(r => !r.IsActive && r.ResourceType == ClusterResourceType.Vm).ToString();
        _lblNodes.Text = nodeCount.ToString();
        _lblVMs.Text = _allRows.Count(r => r.ResourceType == ClusterResourceType.Vm && r.VmType == VmType.Qemu).ToString();
        _lblCTs.Text = _allRows.Count(r => r.ResourceType == ClusterResourceType.Vm && r.VmType == VmType.Lxc).ToString();
    }

    internal void ApplyFilter()
    {
        var filtered = _allRows.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_filterText))
        {
            var ft = _filterText.Trim().ToLowerInvariant();
            filtered = filtered.Where(r => r.Name.Contains(ft, StringComparison.OrdinalIgnoreCase)
                                            || r.IdDisplay.Contains(ft, StringComparison.OrdinalIgnoreCase)
                                            || r.Description.Contains(ft, StringComparison.OrdinalIgnoreCase));
        }

        var filterByStatus = _chkRunning.IsChecked == true || _chkStopped.IsChecked == true;
        if (filterByStatus)
        {
            filtered = filtered.Where(r => r.ResourceType == ClusterResourceType.Node
                                            || (_chkRunning.IsChecked == true && r.IsActive)
                                            || (_chkStopped.IsChecked == true && !r.IsActive));
        }

        var filterByType = _chkQemu.IsChecked == true || _chkLxc.IsChecked == true;
        if (filterByType)
        {
            filtered = filtered.Where(r => r.ResourceType == ClusterResourceType.Node
                                            || (r.VmType == VmType.Qemu && _chkQemu.IsChecked == true)
                                            || (r.VmType == VmType.Lxc && _chkLxc.IsChecked == true));
        }

        if (_filterNodes.Count > 0)
        {
            filtered = filtered.Where(r => r.ResourceType == ClusterResourceType.Node
                                            ? _filterNodes.Contains(r.Name)
                                            : _filterNodes.Contains(r.NodeName));
        }

        if (_filterPools.Count > 0)
        {
            filtered = filtered.Where(r => r.ResourceType == ClusterResourceType.Node
                                            || _filterPools.Contains(r.Pool));
        }

        if (_filterTags.Count > 0)
        {
            filtered = filtered.Where(r => r.Tags.Length == 0
                                            || r.Tags.Any(t => _filterTags.Contains(t)));
        }

        var list = filtered.Where(r => r.HasAnyVdiAction).ToList();
        RebuildCardView(list);
        RebuildListView(list);
    }
}
