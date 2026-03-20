/*
 * SPDX-FileCopyrightText: Copyright Corsinvest Srl
 * SPDX-License-Identifier: MIT
 */

using System.Text.RegularExpressions;
using Corsinvest.ProxmoxVE.Api.Extension;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Cluster;
using Corsinvest.ProxmoxVE.Api.Shared.Models.Vm;
using Corsinvest.ProxmoxVE.Vdi.Services;
using Corsinvest.ProxmoxVE.Vdi.UI.Models;

namespace Corsinvest.ProxmoxVE.Vdi.UI;

internal partial class MainWindow
{
    internal async Task RefreshAsync()
    {
        if (_isRefreshing) { return; }

        _isRefreshing = true;

        _toolbar?.IsEnabled = false;

        _progressBar.IsVisible = true;
        _progressBar.Value = 0;

        try
        {
            // 1. tag color map + permissions
            try { _tagColorMap = (await _client.Cluster.Options.GetAsync())?.TagStyle?.ColorMap ?? string.Empty; }
            catch { _tagColorMap = string.Empty; }

            if (_permissions.Count == 0)
            {
                try { _permissions = await _client.Access.Permissions.GetPermissionsAsync(); }
                catch { _permissions = new Dictionary<string, IReadOnlyList<string>>(); }
            }

            _progressBar.Value = 10;

            // 2. cluster resources
            try { _pveVersion = (await _client.Version.GetAsync())?.Version ?? string.Empty; } catch { }
            var resources = await _client.Cluster.Resources.GetAsync();

            var nodes = resources.Where(r => r.ResourceType == ClusterResourceType.Node)
                                 .OrderBy(r => r.Node)
                                 .ToList();

            var vms = resources.Where(r => r.ResourceType == ClusterResourceType.Vm && !r.IsUnknown)
                               .OrderBy(r => r.Node).ThenBy(r => r.VmId)
                               .ToList();

            _progressBar.Value = 20;

            // invalidate RDP cache for VMs no longer running
            var runningIds = vms.Where(v => v.IsRunning).Select(v => v.VmId).ToHashSet();
            foreach (var id in _rdpCache.Keys.Where(id => !runningIds.Contains(id)).ToList())
            {
                _rdpCache.Remove(id);
            }

            // invalidate SPICE cache for VMs back to running (config may have changed)
            foreach (var id in _spiceConfigCache.Keys.Where(id => runningIds.Contains(id)).ToList())
            {
                _spiceConfigCache.Remove(id);
            }

            _allRows.Clear();

            // 3. Nodes + LXC — shown immediately, no extra API calls
            foreach (var item in nodes)
            {
                var privs = EffectivePrivs($"/nodes/{item.Node}").ToHashSet();
                _allRows.Add(new ResourceRow(item, false, false, null, false, privs.Contains("Sys.Console"), string.Empty));
            }

            foreach (var item in vms.Where(v => v.VmType == VmType.Lxc).ToList())
            {
                var privs = EffectivePrivs($"/vms/{item.VmId}").ToHashSet();
                var canPower = privs.Contains("VM.PowerMgmt");
                var canConsole = privs.Contains("VM.Console");
                var hasSpice = _config.EnableSpice && item.IsRunning && canConsole;
                _allRows.Add(new ResourceRow(item, hasSpice, false, null, canPower, canConsole, "linux"));
            }

            // rebuild node filter checkboxes
            void ToggleFilter(HashSet<string> set, string value, bool isChecked)
            {
                if (isChecked)
                {
                    set.Add(value);
                }
                else
                {
                    set.Remove(value);
                }

                ApplyFilter();
            }

            var knownNodes = _nodeFilters.Children.OfType<CheckBox>().Select(c => c.Tag as string).ToHashSet();
            foreach (var item in nodes.Where(n => !knownNodes.Contains(n.Node)))
            {
                var chk = new CheckBox
                {
                    Tag = item.Node,
                    Content = item.Node,
                    IsChecked = false
                };
                chk.IsCheckedChanged += (_, _) => ToggleFilter(_filterNodes, item.Node!, chk.IsChecked == true);
                _nodeFilters.Children.Add(chk);
            }

            UpdateStats(nodes.Count);
            ApplyFilter();
            _progressBar.Value = 30;

            // 4. QEMU — parallel chunks of 5, progressive
            var qemuVms = vms.Where(v => v.VmType == VmType.Qemu).ToList();
            // SPICE detection requires API calls; VNC is always available (no API needed)
            var qemuToCheck = _config.EnableSpice
                                ? qemuVms.Where(v => v.IsRunning || !_spiceConfigCache.ContainsKey(v.VmId)).ToList()
                                : [];
            var totalQemu = qemuToCheck.Count;
            var doneQemu = 0;

            // add all QEMU as placeholders first, SPICE/RDP resolved progressively
            foreach (var item in qemuVms)
            {
                var privs = EffectivePrivs($"/vms/{item.VmId}").ToHashSet();
                var canPower = privs.Contains("VM.PowerMgmt");
                var canConsole = privs.Contains("VM.Console");
                var osType = _osTypeCache.GetValueOrDefault(item.VmId, string.Empty);
                var hasSpice = _config.EnableSpice && _spiceConfigCache.GetValueOrDefault(item.VmId, false);
                var (hasRdp, rdpIp) = _rdpCache.GetValueOrDefault(item.VmId);
                var features = _featuresCache.GetValueOrDefault(item.VmId, SpiceFeatures.None);
                _allRows.Add(new ResourceRow(item, hasSpice, hasRdp, rdpIp, canPower, canConsole, osType, features));
            }

            UpdateStats(nodes.Count);
            ApplyFilter();

            foreach (var chunk in qemuToCheck.Chunk(5))
            {
                await Task.WhenAll(chunk.Select(async v =>
                {
                    try
                    {
                        bool hasSpice;
                        string osType;
                        SpiceFeatures features;

                        if (v.IsRunning)
                        {
                            var vm = _client.Nodes[v.Node].Qemu[v.VmId];

                            static async Task<bool> PingWithTimeout(Task<Api.Result> pingTask, int timeoutMs)
                            {
                                var timeout = Task.Delay(timeoutMs);
                                var completed = await Task.WhenAny(pingTask, timeout);
                                if (completed == timeout) { return false; }
                                return pingTask.Result?.IsSuccessStatusCode == true;
                            }

                            if (_osTypeCache.ContainsKey(v.VmId))
                            {
                                var stTask = vm.Status.Current.GetAsync();
                                await stTask;
                                hasSpice = stTask.Result?.Spice == true;
                                osType = _osTypeCache[v.VmId];
                                var cached = _featuresCache.GetValueOrDefault(v.VmId, SpiceFeatures.None);

                                bool agentRunning;
                                if (!_config.EnableAgentPing)
                                {
                                    agentRunning = false;
                                }
                                else if (_agentPingCache.TryGetValue(v.VmId, out var pingEntry)
                                      && (DateTime.Now - pingEntry.CheckedAt).TotalSeconds < AgentPingCacheSeconds)
                                {
                                    agentRunning = pingEntry.Running;
                                }
                                else
                                {
                                    agentRunning = await PingWithTimeout(vm.Agent.Ping.Ping(), AgentPingTimeoutMs);
                                    _agentPingCache[v.VmId] = (agentRunning, DateTime.Now);
                                }

                                features = cached with { AgentRunning = agentRunning };
                            }
                            else
                            {
                                var stTask = vm.Status.Current.GetAsync();
                                var cfgTask = vm.Config.GetAsync();
                                await Task.WhenAll(stTask, cfgTask);
                                hasSpice = stTask.Result?.Spice == true;
                                var cfg = cfgTask.Result;
                                osType = cfg?.OsType?.ToLowerInvariant() ?? string.Empty;
                                _osTypeCache[v.VmId] = osType;
                                bool agentRunning;
                                if (!_config.EnableAgentPing)
                                {
                                    agentRunning = false;
                                }
                                else if (_agentPingCache.TryGetValue(v.VmId, out var pingEntry)
                                            && (DateTime.Now - pingEntry.CheckedAt).TotalSeconds < AgentPingCacheSeconds)
                                {
                                    agentRunning = pingEntry.Running;
                                }
                                else
                                {
                                    agentRunning = await PingWithTimeout(vm.Agent.Ping.Ping(), AgentPingTimeoutMs);
                                    _agentPingCache[v.VmId] = (agentRunning, DateTime.Now);
                                }

                                features = BuildFeatures(cfg, agentRunning);
                                _featuresCache[v.VmId] = features;
                            }
                        }
                        else
                        {
                            var cfg = await _client.Nodes[v.Node].Qemu[v.VmId].Config.GetAsync();
                            var vga = cfg?.Vga?.ToLowerInvariant() ?? string.Empty;
                            hasSpice = vga.StartsWith("qxl") || vga == "spice";
                            osType = cfg?.OsType?.ToLowerInvariant() ?? string.Empty;
                            _spiceConfigCache[v.VmId] = hasSpice;
                            _osTypeCache[v.VmId] = osType;
                            features = BuildFeatures(cfg, false);
                            _featuresCache[v.VmId] = features;
                        }

                        // update existing row in _allRows
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            var idx = _allRows.FindIndex(r => r.Resource.VmId == v.VmId);
                            if (idx >= 0)
                            {
                                var old = _allRows[idx];
                                _allRows[idx] = new ResourceRow(v,
                                                                hasSpice,
                                                                old.HasRdp,
                                                                old.RdpIp,
                                                                old.CanPower,
                                                                old.CanConsole,
                                                                osType,
                                                                features);
                            }
                        });
                    }
                    catch { }
                }));

                doneQemu += chunk.Length;
                _progressBar.Value = 30 + (doneQemu * 50 / Math.Max(totalQemu, 1));
                UpdateStats(nodes.Count);
                ApplyFilter();
            }

            _progressBar.Value = 80;

            // 5. RDP — only if enabled for this host, only running QEMU not cached, chunks of 5
            var rdpToCheck = _config.EnableRdp
                                ? qemuVms.Where(v => v.IsRunning && !_rdpCache.ContainsKey(v.VmId)).ToList()
                                : [];
            var totalRdp = rdpToCheck.Count;
            var doneRdp = 0;

            foreach (var chunk in rdpToCheck.Chunk(5))
            {
                await Task.WhenAll(chunk.Select(async v =>
                {
                    try
                    {
                        var ip = await VmService.GetVmIpAsync(_client, v.Node, v.VmId);
                        if (ip is null)
                        {
                            _rdpCache[v.VmId] = (false, null);
                            return;
                        }

                        var hasRdp = await VmService.IsRdpOpenAsync(ip);
                        _rdpCache[v.VmId] = (hasRdp, hasRdp
                                                ? ip
                                                : null);

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            var idx = _allRows.FindIndex(r => r.Resource.VmId == v.VmId);
                            if (idx >= 0)
                            {
                                var old = _allRows[idx];
                                _allRows[idx] = new ResourceRow(v,
                                                                old.CanSpice,
                                                                hasRdp,
                                                                hasRdp ? ip : null,
                                                                old.CanPower,
                                                                old.CanConsole,
                                                                old.OsType);
                            }
                        });
                    }
                    catch { _rdpCache[v.VmId] = (false, null); }
                }));

                doneRdp += chunk.Length;
                _progressBar.Value = 80 + (doneRdp * 18 / Math.Max(totalRdp, 1));
                ApplyFilter();
            }

            // rebuild pool filter — only if enabled and only pools of VDI-actionable VMs
            if (_config.ShowPools)
            {
                var allPools = _allRows
                    .Where(r => r.HasAnyVdiAction && !string.IsNullOrEmpty(r.Pool))
                    .Select(r => r.Pool)
                    .Distinct()
                    .OrderBy(p => p)
                    .ToList();
                var knownPools = _poolFilters.Children.OfType<CheckBox>().Select(c => c.Tag as string).ToHashSet();
                foreach (var pool in allPools.Where(p => !knownPools.Contains(p)))
                {
                    var chk = new CheckBox
                    {
                        Tag = pool,
                        Content = pool,
                        IsChecked = false
                    };
                    chk.IsCheckedChanged += (_, _) => ToggleFilter(_filterPools, pool, chk.IsChecked == true);
                    _poolFilters.Children.Add(chk);
                }
            }

            // rebuild tag filters — only if enabled and only tags of VDI-actionable VMs
            if (_config.ShowTags)
            {
                var allTags = _allRows.Where(r => r.HasAnyVdiAction).SelectMany(r => r.Tags).Distinct().OrderBy(t => t).ToList();
                var existingTags = _tagFilters.Children.OfType<CheckBox>().Select(c => c.Tag as string).ToHashSet();
                foreach (var tag in allTags.Where(t => !existingTags.Contains(t)))
                {
                    var chk = new CheckBox
                    {
                        Tag = tag,
                        Content = tag,
                        IsChecked = false
                    };
                    chk.IsCheckedChanged += (_, _) => ToggleFilter(_filterTags, tag, chk.IsChecked == true);
                    _tagFilters.Children.Add(chk);
                }
            }

            ApplyFilter();
            _toolbar?.IsEnabled = true;
            _progressBar.Value = 100;
        }
        catch (Exception ex)
        {
            ShowToast($"{L("ErrorPrefix")}{ex.Message}", NotificationSeverity.Error);
        }
        finally
        {
            _progressBar.IsVisible = false;
            _isRefreshing = false;
            _toolbar?.IsEnabled = true;
        }
    }

    internal async Task LaunchVncAsync(ResourceRow row)
    {
        var err = await RemoteViewerService.LaunchVncAsync(_client,
                                                           row.Resource.Node,
                                                           row.Resource.VmId,
                                                           row.VmType,
                                                           _config,
                                                           _client.PVEAuthCookie);
        if (!string.IsNullOrEmpty(err))
        {
            ShowToast($"{L("ErrorPrefix")}{err}", NotificationSeverity.Error);
        }
    }

    internal async Task LaunchSpiceAsync(ResourceRow row)
    {
        string err;
        if (row.ResourceType == ClusterResourceType.Node)
        {
            err = await RemoteViewerService.LaunchNodeSpiceAsync(_client, row.Resource.Node, _config, _host);
        }
        else
        {
            err = await RemoteViewerService.LaunchSpiceAsync(_client,
                                                             row.Resource.Node,
                                                             row.Resource.VmId,
                                                             row.VmType,
                                                             _config,
                                                             _host);
        }

        if (!string.IsNullOrEmpty(err))
        {
            ShowToast($"{L("ErrorPrefix")}{err}", NotificationSeverity.Error);
        }
    }

    private static SpiceFeatures BuildFeatures(VmConfigQemu? cfg, bool agentRunning)
    {
        if (cfg is null) { return SpiceFeatures.None; }

        var audio = cfg.Audio0?.Contains("driver=spice") == true;
        var usbRedirect = cfg.ExtensionData
                             ?.Where(kv => UsbRegex().IsMatch(kv.Key))
                             .Any(kv => kv.Value?.ToString()?.Contains("host=spice") == true) == true;

        var clipboard = cfg.SpiceEnhancements?.Contains("clipboard") == true;
        return new SpiceFeatures(audio, usbRedirect, cfg.AgentEnabled, agentRunning, clipboard);
    }

    internal async Task<bool> ConfirmAsync(string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        var dlg = new Window
        {
            Title = L("Confirm"),
            Width = 320,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false
        };

        var btnYes = new Button
        {
            Content = L("Yes"),
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var btnNo = new Button
        {
            Content = L("No"),
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        btnYes.Click += (_, _) =>
        {
            tcs.TrySetResult(true);
            dlg.Close();
        };

        btnNo.Click += (_, _) =>
        {
            tcs.TrySetResult(false);
            dlg.Close();
        };

        dlg.Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 12,
                    Children = { btnNo, btnYes }
                }
            }
        };
        dlg.Closed += (_, _) => tcs.TrySetResult(false);
        await dlg.ShowDialog(_window!);
        return await tcs.Task;
    }

    [GeneratedRegex(@"^usb\d+$")]
    private static partial Regex UsbRegex();
}
