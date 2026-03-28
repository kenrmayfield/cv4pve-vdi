/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Helpers;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    private Control BuildHelpMenu(MenuItem menuItemSettings)
    {
        var updateBadge = new Ellipse
        {
            Width = 7,
            Height = 7,
            Fill = new SolidColorBrush(Colors.OrangeRed),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            IsVisible = false,
            IsHitTestVisible = false,
            Margin = new Thickness(0, -2, -2, 0)
        };

        var miUpdate = new MenuItem
        {
            Header = UiHelper.WithText(AppIcons.Update, L("UpdateAvailable"), new SolidColorBrush(Colors.OrangeRed)),
            IsVisible = false
        };

        var menuItemDocs = new MenuItem { Header = UiHelper.WithText(AppIcons.Book, L("Documentation")) };
        var menuItemRelease = new MenuItem { Header = UiHelper.WithText(AppIcons.Star, L("ReleaseNotes")) };
        var menuItemSupport = new MenuItem { Header = UiHelper.WithText(AppIcons.Globe, L("Support")) };
        var menuItemBug = new MenuItem { Header = UiHelper.WithText(AppIcons.Bug, L("ReportBug")) };
        var menuItemFeature = new MenuItem { Header = UiHelper.WithText(AppIcons.Info, L("RequestFeature")) };

        var versionHeader = new TextBlock
        {
            Text = $"cv4pve-vdi  v{ApplicationHelper.Version}",
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(8, 4),
            Opacity = 0.7,
            FontSize = 12
        };

        var menu = new ContextMenu
        {
            Items =
            {
                new MenuItem { Header = versionHeader, IsEnabled = false },
                miUpdate,
                new Separator(),
                menuItemSettings,
                new Separator(),
                menuItemDocs,
                menuItemRelease,
                menuItemSupport,
                new Separator(),
                menuItemBug,
                menuItemFeature,
            }
        };

        menuItemDocs.Click += (_, _) => OpenUrl(ApplicationHelper.DocumentationUrl);
        menuItemRelease.Click += (_, _) => OpenUrl(ApplicationHelper.ReleaseNotesUrl);
        menuItemSupport.Click += (_, _) => OpenUrl(ApplicationHelper.SupportUrl);
        menuItemBug.Click += (_, _) => OpenUrl(ApplicationHelper.GetBugReportUrl(_pveVersion));
        menuItemFeature.Click += (_, _) => OpenUrl(ApplicationHelper.FeatureRequestUrl);

        var btn = UiHelper.IconButton(AppIcons.DotsVertical, margin: new Thickness(4, 0, 0, 0));

        var btnGrid = new Grid();
        btnGrid.Children.Add(UiHelper.Icon(AppIcons.DotsVertical));
        btnGrid.Children.Add(updateBadge);
        btn.Content = btnGrid;

        // show badge when update is available
        miUpdate.PropertyChanged += (_, e) =>
        {
            if (e.Property.Name == "IsVisible") { updateBadge.IsVisible = miUpdate.IsVisible; }
        };

        Avalonia.Controls.ToolTip.SetTip(btn, L("More"));
        btn.Click += (_, _) => menu.Open(btn);

        UpdateChecker.StartBackground((version, url) =>
        {
            miUpdate.Header = UiHelper.WithText(AppIcons.Update, $"{version} {L("UpdateAvailable")}", new SolidColorBrush(Colors.OrangeRed));
            miUpdate.IsVisible = true;
            miUpdate.Click += (_, _) => OpenUrl(url);
        });

        return btn;
    }
}
