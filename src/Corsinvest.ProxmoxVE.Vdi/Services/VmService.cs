/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Net.Sockets;
using System.Runtime.InteropServices;
using Corsinvest.ProxmoxVE.Api;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Extension.Utils;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

internal static class VmService
{
    public static async Task ChangeStatusAsync(PveClient client, string node, long vmId, VmType vmType, VmStatus status)
        => await VmHelper.ChangeStatusVmAsync(client, node, vmType, vmId, status);

    /// <summary>
    /// Gets the first non-loopback IPv4 of a QEMU VM via guest agent.
    /// Returns null if agent is not available or no IP found.
    /// </summary>
    public static async Task<string?> GetVmIpAsync(PveClient client, string node, long vmId)
    {
        try
        {
            var ifaces = await client.Nodes[node].Qemu[vmId].Agent.NetworkGetInterfaces.GetAsync();
            return ifaces?.Result.SelectMany(i => i.IpAddresses)
                                 .FirstOrDefault(a => a.IpAddressType == "ipv4" && !a.IpAddress.StartsWith("127."))
                                 ?.IpAddress;
        }
        catch { return null; }
    }

    /// <summary>
    /// TCP-connect check on port 3389 with a short timeout.
    /// </summary>
    public static async Task<bool> IsRdpOpenAsync(string ip, int timeoutMs = 600)
    {
        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(ip, 3389).WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// Launches mstsc.exe (Windows) or xfreerdp (Linux/Mac) to connect via RDP.
    /// </summary>
    public static string LaunchRdp(string ip, string rdpPath)
    {
        try
        {
            var startInfo = new ProcessStartInfo { UseShellExecute = true };
            if (!string.IsNullOrWhiteSpace(rdpPath))
            {
                startInfo.FileName = rdpPath;
                startInfo.Arguments = $"/v:{ip}";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                startInfo.FileName = "mstsc.exe";
                startInfo.Arguments = $"/v:{ip}";
            }
            else
            {
                startInfo.FileName = "xfreerdp";
                startInfo.Arguments = $"/v:{ip}";
            }

            Process.Start(startInfo);
            return string.Empty;
        }
        catch (Exception ex) { return ex.Message; }
    }
}
