using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using Sharpcaster;
using Sharpcaster.Models;

namespace Sabeltann.Services;

/// <summary>
/// Cast device <b>discovery</b> — mDNS (SharpCaster) plus a unicast subnet scan, merged. The actual
/// streaming is done by libvlc's chromecast output (<see cref="PlaybackService.CastToIp"/>), addressed
/// by the IP this discovery returns, so casting needs no mDNS and plays any codec via libvlc transcode.
/// Also owns the one-time Windows Firewall rule that lets mDNS replies reach the app.
/// </summary>
public class CastService
{
    private const string FirewallRuleName = "Sabeltann mDNS discovery";

    /// <summary>
    /// Ensures an inbound Windows Firewall rule for mDNS (UDP 5353) exists so Cast discovery receives
    /// responses. Without it Windows silently drops the replies on the Public profile — by default only
    /// svchost (and apps with their own rule, e.g. Chrome/Edge) may receive — so no devices are ever
    /// found. Adds the rule via an elevated <c>netsh</c> call: one UAC prompt the first run, a no-op
    /// afterwards. Best-effort — if the rule can't be added (user declines UAC), discovery just stays
    /// empty, so callers still degrade gracefully. Port-scoped (not program-scoped) so Velopack's
    /// per-version install path can change on update without stranding the rule. Windows-only.
    /// </summary>
    public static void EnsureMdnsFirewallRule()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            if (MdnsRuleExists()) return;
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall add rule name=\"{FirewallRuleName}\" " +
                            "dir=in action=allow protocol=UDP localport=5353 profile=any enable=yes",
                UseShellExecute = true,   // required for Verb=runas
                Verb = "runas",           // elevate (UAC)
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
            Process.Start(psi)?.WaitForExit(15_000);
            LogService.Info("mDNS firewall rule add attempted");
        }
        catch (Exception ex)
        {
            LogService.Warn("Could not add mDNS firewall rule (discovery may find nothing)", new { error = ex.Message });
        }
    }

    private static bool MdnsRuleExists()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = $"advfirewall firewall show rule name=\"{FirewallRuleName}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            var output = p.StandardOutput.ReadToEnd();
            p.WaitForExit(5_000);
            // netsh exits 1 + "No rules match the specified criteria." when the rule is absent.
            return p.ExitCode == 0 && !output.Contains("No rules match", StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    // Cast control port; the eureka_info HTTP endpoint sits on port-1 (8008).
    private const int CastPort = 8009;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(2) };

    /// <summary>
    /// Discovers Cast receivers by running standard mDNS and a unicast subnet scan in parallel, then
    /// merging by host. mDNS is the correct primary; the scan is the fallback for networks that filter
    /// multicast (VPN tunnels, guest/enterprise Wi-Fi, some consumer APs) where mDNS finds nothing even
    /// though the device answers unicast fine. Either source yields a directly-connectable receiver.
    /// </summary>
    public async Task<IReadOnlyList<ChromecastReceiver>> FindDevicesAsync(TimeSpan timeout)
    {
        var lists = await Task.WhenAll(FindViaMdnsAsync(timeout), FindViaScanAsync());
        var byHost = new Dictionary<string, ChromecastReceiver>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in lists.SelectMany(x => x))
            if (r.DeviceUri is not null)
                byHost[r.DeviceUri.Host] = r;   // dedupe across sources
        return byHost.Values.ToList();
    }

    private static async Task<IReadOnlyList<ChromecastReceiver>> FindViaMdnsAsync(TimeSpan timeout)
    {
        try { return (await new ChromecastLocator().FindReceiversAsync(timeout)).ToList(); }
        catch (Exception ex) { LogService.Warn("mDNS cast discovery failed", new { error = ex.Message }); return []; }
    }

    // Scans the host's own /24 for the Cast port and names each hit via its eureka_info endpoint.
    // ponytail: /24 only — widen to the interface's real mask if devices ever sit outside the host's /24.
    private static async Task<IReadOnlyList<ChromecastReceiver>> FindViaScanAsync()
    {
        var local = LocalLanIPv4();
        if (local is null) return [];
        var prefix = local[..(local.LastIndexOf('.') + 1)];
        var found = new ConcurrentBag<ChromecastReceiver>();
        await Task.WhenAll(Enumerable.Range(1, 254).Select(async i =>
        {
            var ip = prefix + i;
            if (!await PortOpenAsync(ip, CastPort, 400)) return;
            found.Add(new ChromecastReceiver
            {
                DeviceUri = new Uri($"https://{ip}"),
                Port = CastPort,
                Name = await EurekaNameAsync(ip) ?? ip,
            });
        }));
        return found.ToList();
    }

    private static async Task<bool> PortOpenAsync(string ip, int port, int timeoutMs)
    {
        using var c = new TcpClient();
        try
        {
            var connect = c.ConnectAsync(ip, port);
            if (await Task.WhenAny(connect, Task.Delay(timeoutMs)) != connect) return false;
            await connect;            // observe result/exception
            return c.Connected;
        }
        catch { return false; }
    }

    private static async Task<string?> EurekaNameAsync(string ip)
    {
        try
        {
            var json = await Http.GetStringAsync($"http://{ip}:8008/setup/eureka_info");
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() : null;
        }
        catch { return null; }
    }

    // IPv4 of the interface that actually has a default gateway — the real LAN, skipping VPN tunnels
    // (Tailscale et al. have no IPv4 gateway) so the scan runs on the subnet the device is on.
    internal static string? LocalLanIPv4() =>
        (from ni in NetworkInterface.GetAllNetworkInterfaces()
         where ni.OperationalStatus == OperationalStatus.Up
         let p = ni.GetIPProperties()
         where p.GatewayAddresses.Any(g => g.Address.AddressFamily == AddressFamily.InterNetwork)
         from ua in p.UnicastAddresses
         where ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address)
         select ua.Address.ToString()).FirstOrDefault();
}
