using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace LiteQMS.Services;

public class UdpDiscoveryService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<UdpDiscoveryService> _logger;
    private const int DiscoveryPort = 56789;
    private static readonly byte[] MagicRequest = Encoding.UTF8.GetBytes("LITEQMS_DISCOVER");

    public UdpDiscoveryService(IConfiguration config, ILogger<UdpDiscoveryService> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        UdpClient? udp = null;
        try
        {
            udp = new UdpClient(new IPEndPoint(IPAddress.Any, DiscoveryPort));
            _logger.LogInformation("UDP discovery listening on port {Port}", DiscoveryPort);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UDP discovery on port {Port} failed — service disabled", DiscoveryPort);
            return;
        }

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                UdpReceiveResult result;
                try
                {
                    result = await udp.ReceiveAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "UDP receive error");
                    continue;
                }

                if (!result.Buffer.AsSpan().SequenceEqual(MagicRequest))
                    continue;

                try
                {
                    var ip = _config["LiteQMS:PrimaryIP"] ?? GetLocalIP();
                    var port = _config["LiteQMS:Port"] ?? "5000";
                    var response = JsonSerializer.Serialize(new
                    {
                        ServerName = Environment.MachineName,
                        IP = ip,
                        Port = int.Parse(port)
                    });

                    var bytes = Encoding.UTF8.GetBytes(response);
                    await udp.SendAsync(bytes, bytes.Length, result.RemoteEndPoint);
                    _logger.LogInformation("Discovery response sent to {Endpoint}", result.RemoteEndPoint);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send discovery response to {Endpoint}", result.RemoteEndPoint);
                }
            }
        }
        finally
        {
            udp.Dispose();
        }
    }

    private static string GetLocalIP()
    {
        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;

            var desc = ni.Description;
            if (desc.Contains("Tailscale", StringComparison.OrdinalIgnoreCase) ||
                desc.Contains("Docker", StringComparison.OrdinalIgnoreCase) ||
                desc.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) ||
                desc.Contains("VMware", StringComparison.OrdinalIgnoreCase) ||
                desc.Contains("Hyper-V", StringComparison.OrdinalIgnoreCase))
                continue;

            var ipProps = ni.GetIPProperties();
            var hasGateway = ipProps.GatewayAddresses.Count > 0;

            foreach (var addr in ipProps.UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(addr.Address) && hasGateway)
                {
                    return addr.Address.ToString();
                }
            }
        }

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            foreach (var addr in ni.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(addr.Address))
                {
                    return addr.Address.ToString();
                }
            }
        }

        return "127.0.0.1";
    }
}
