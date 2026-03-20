/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;

namespace Corsinvest.ProxmoxVE.Vdi.Services;

/// <summary>
/// Bridges a Proxmox VNC WebSocket to a local TCP port so that
/// remote-viewer (which speaks plain TCP VNC) can connect.
/// </summary>
internal sealed class VncWebSocketBridge : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private Task? _acceptLoop;

    public int LocalPort { get; }

    public VncWebSocketBridge()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        LocalPort = ((IPEndPoint)_listener.LocalEndpoint).Port;
    }

    public void Start(string wsUrl, string host, string pveAuthCookie)
        => _acceptLoop = AcceptLoopAsync(wsUrl, host, pveAuthCookie, _cts.Token);

    private async Task AcceptLoopAsync(string wsUrl, string host, string pveAuthCookie, CancellationToken ct)
    {
        try
        {
            var ws = new ClientWebSocket();
            ws.Options.AddSubProtocol("binary");
            var cookies = new CookieContainer();
            cookies.Add(new Cookie("PVEAuthCookie", pveAuthCookie, "/", host) { Secure = true });
            ws.Options.Cookies = cookies;
            ws.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;

            await ws.ConnectAsync(new Uri(wsUrl), ct);

            using var tcp = await _listener.AcceptTcpClientAsync(ct);
            tcp.NoDelay = true;

            var stream = tcp.GetStream();
            await Task.WhenAny(TcpToWsAsync(stream, ws, ct), WsToTcpAsync(ws, stream, ct));
        }
        catch { }
    }

    private static async Task TcpToWsAsync(NetworkStream src, ClientWebSocket dst, CancellationToken ct)
    {
        var buf = new byte[65536];
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var read = await src.ReadAsync(buf, ct);
                if (read == 0)
                {
                    break;
                }

                await dst.SendAsync(buf.AsMemory(0, read), WebSocketMessageType.Binary, true, ct);
            }
        }
        catch { }
    }

    private static async Task WsToTcpAsync(ClientWebSocket src, NetworkStream dst, CancellationToken ct)
    {
        var buf = new byte[65536];
        try
        {
            while (!ct.IsCancellationRequested && src.State == WebSocketState.Open)
            {
                var result = await src.ReceiveAsync(buf.AsMemory(), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                await dst.WriteAsync(buf.AsMemory(0, result.Count), ct);
            }
        }
        catch { }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        if (_acceptLoop != null) { await _acceptLoop.ConfigureAwait(false); }
        _cts.Dispose();
    }
}
