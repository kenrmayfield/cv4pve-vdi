/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal static partial class SettingsWindow
{
    private static (TabItem Tab, Action? Save) BuildTabClusters(AppConfig config, Window owner, Action? onHostsChanged)
    {
        Action refresh = null!;

        var toolbarButtons = new List<ToolbarButton>
        {
            new()
            {
                Icon    = AppIcons.Add,
                Tooltip = L("Add"),
                OnClick = () =>
                {
                    var dlg = ClusterEditWindow.Create(null);
                    dlg.Icon = owner.Icon;
                    dlg.ShowDialog<ClusterConfig?>(owner).ContinueWith(t =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            var result = t.Result;
                            if (result is null) { return; }
                            config.Clusters.Add(result);
                            refresh();
                            onHostsChanged?.Invoke();
                        });
                    });
                    return Task.CompletedTask;
                }
            }
        };

        var rowButtons = new List<RowButton<ClusterConfig>>
        {
            new()
            {
                Icon          = AppIcons.Edit,
                Tooltip       = L("Edit"),
                IsDoubleClick = true,
                OnClick       = cluster =>
                {
                    var idx = config.Clusters.IndexOf(cluster);
                    if (idx < 0) { return Task.CompletedTask; }

                    var dlg = ClusterEditWindow.Create(cluster);
                    dlg.Icon = owner.Icon;
                    dlg.ShowDialog<ClusterConfig?>(owner).ContinueWith(t =>
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            var result = t.Result;
                            if (result is null) { return; }
                            config.Clusters[idx] = result;
                            refresh();
                            onHostsChanged?.Invoke();
                        });
                    });
                    return Task.CompletedTask;
                }
            },
            new()
            {
                Icon       = AppIcons.Delete,
                Tooltip    = L("Delete"),
                Foreground = Brushes.IndianRed,
                OnClick    = cluster =>
                {
                    config.Clusters.Remove(cluster);
                    refresh();
                    onHostsChanged?.Invoke();
                    return Task.CompletedTask;
                }
            }
        };

        var (listPanel, refreshFn) = ActionListBox.Build(config.Clusters,
                                                         c => c.Name,
                                                         toolbarButtons,
                                                         rowButtons,
                                                         visibleRows: 6);

        refresh = refreshFn;

        var tab = new TabItem
        {
            Header = UiHelper.WithText(AppIcons.Server, L("TabClusters")),
            Content = new StackPanel
            {
                Margin = new Thickness(0, 12, 0, 0),
                Spacing = 8,
                Children =
                {
                    SectionHeader(L("SectionClusters")),
                    listPanel,
                }
            }
        };

        return (tab, null);
    }
}
