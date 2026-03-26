/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Api.Shared.Utils;
using Corsinvest.ProxmoxVE.Vdi.Config.Models;
using System.Text.RegularExpressions;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

internal static partial class RemoteViewerService
{
    [GeneratedRegex("^(http|https|)://.*$")]
    private static partial Regex ProxyUrlRegex();

    private static string ResolveProxy(ClusterConfig host, PveClient client)
        => string.IsNullOrEmpty(host.Spice.Proxy) ? client.Host : host.Spice.Proxy;

    public static async Task<string> LaunchSpiceAsync(PveClient client, string node, long vmId, VmType vmType, AppConfig config, ClusterConfig host)
    {
        var proxy = ResolveProxy(host, client);

        var (success, reasonPhrase, content) = vmType == VmType.Lxc
                                                ? await client.Nodes[node].Lxc[vmId].Spiceproxy.GetSpiceFileVVAsync(proxy)
                                                : await client.Nodes[node].Qemu[vmId].Spiceproxy.GetSpiceFileVVAsync(proxy);

        if (!success) { return reasonPhrase ?? "SPICE proxy request failed"; }

        return LaunchViewer(OverrideProxy(content, proxy), config, host);
    }

    public static async Task<string> LaunchNodeSpiceAsync(PveClient client, string node, AppConfig config, ClusterConfig host)
    {
        var proxy = ResolveProxy(host, client);

        var (success, reasonPhrase, content) = await client.Nodes[node].Spiceshell.GetSpiceFileVVAsync(proxy);
        if (!success) { return reasonPhrase ?? "SPICE shell request failed"; }

        return LaunchViewer(OverrideProxy(content, proxy), config, host);
    }

    private static string OverrideProxy(string content, string proxy)
    {
        if (!ProxyUrlRegex().IsMatch(proxy)) { return content; }

        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].StartsWith("proxy="))
            {
                lines[i] = $"proxy={proxy}";
                break;
            }
        }

        return string.Join('\n', lines);
    }

    public static async Task<string> LaunchVncAsync(PveClient client, string node, long vmId, VmType vmType, AppConfig config, string pveAuthCookie)
    {
        if (string.IsNullOrWhiteSpace(config.ViewerPath))
        {
            return "SPICE viewer path is not configured. Please set it in Settings → Viewer.";
        }

        var result = vmType == VmType.Lxc
                        ? await client.Nodes[node].Lxc[vmId].Vncproxy.Vncproxy(websocket: true)
                        : await client.Nodes[node].Qemu[vmId].Vncproxy.Vncproxy(websocket: true);

        if (!result.IsSuccessStatusCode) { return result.ReasonPhrase ?? "VNC proxy request failed"; }

        var ticket = (string)result.Response.data.ticket;
        var port = Convert.ToInt32(result.Response.data.port);
        var wsUrl = NoVncHelper.GetWebsocketUrl(client.Host,
                                                client.Port,
                                                node,
                                                vmType == VmType.Lxc ? "lxc" : "qemu",
                                                vmId,
                                                port,
                                                System.Web.HttpUtility.UrlEncode(ticket));

        var bridge = new VncWebSocketBridge();
        bridge.Start(wsUrl, client.Host, pveAuthCookie);

        var vvContent = $"""
            [virt-viewer]
            type=vnc
            host=127.0.0.1
            port={bridge.LocalPort}
            password={ticket}
            title={node}:{(vmType == VmType.Lxc ? "lxc" : "qemu")}/{vmId}
            delete-this-file=1
            """;

        var vvFile = System.IO.Path.GetTempFileName().Replace(".tmp", ".vv");
        await File.WriteAllTextAsync(vvFile, vvContent);

        _ = Task.Run(async () =>
        {
            await using (bridge)
            {
                _ = await LaunchAndWaitAsync(vvFile, config);
            }
        });

        return string.Empty;
    }

    private static async Task<string> LaunchAndWaitAsync(string vvFile, AppConfig config)
    {
        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo.FileName = $"\"{config.ViewerPath}\"";
            startInfo.Arguments = $"\"{vvFile}\"";
        }
        else
        {
            startInfo.FileName = "/bin/bash";
            startInfo.Arguments = $"-c \"{config.ViewerPath} {vvFile}\"";
        }

        try
        {
            var process = new Process { StartInfo = startInfo };
            process.Start();
            await process.WaitForExitAsync();
            return string.Empty;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    private static string LaunchViewer(string content, AppConfig config, ClusterConfig host)
    {
        var vvFile = System.IO.Path.GetTempFileName().Replace(".tmp", ".vv");
        File.WriteAllText(vvFile, content);

        var viewerPath = config.ViewerPath;
        if (string.IsNullOrWhiteSpace(viewerPath))
        {
            return "SPICE viewer path is not configured. Please set it in Settings → Viewer.";
        }

        var viewerOptions = host.Spice.ViewerOptions.Replace(Environment.NewLine, " ");
        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            startInfo.FileName = $"\"{viewerPath}\"";
            startInfo.Arguments = $"\"{vvFile}\" {viewerOptions}";
        }
        else
        {
            startInfo.FileName = "/bin/bash";
            startInfo.Arguments = $"-c \"{viewerPath} {vvFile} {viewerOptions}\"";
        }

        try { new Process { StartInfo = startInfo }.Start(); return string.Empty; }
        catch (Exception ex) { return ex.Message; }
    }
}
