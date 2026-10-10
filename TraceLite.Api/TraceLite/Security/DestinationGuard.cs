using System.Net;
using System.Net.Sockets;

namespace TraceLite.Security;

public sealed class BlockedDestinationException : Exception
{
    public BlockedDestinationException(string message)
        : base(message)
    {
    }
}

// Used as the ConnectCallback for HttpClients that call user-supplied URLs.
// The host is resolved here and the socket connects only to the vetted
// addresses, so DNS tricks (rebinding, mixed public/private records) and
// redirects to internal hosts cannot reach the private network.
public static class DestinationGuard
{
    public static async ValueTask<Stream> ConnectAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        var host = context.DnsEndPoint.Host;

        var addresses = IPAddress.TryParse(host, out var literal)
            ? [literal]
            : await Dns.GetHostAddressesAsync(host, cancellationToken);

        var allowed = addresses
            .Where(address => !IsBlocked(address))
            .ToArray();

        if (allowed.Length == 0)
        {
            throw new BlockedDestinationException(
                $"Requests to '{host}' are blocked because it resolves to a private or reserved network address.");
        }

        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
        {
            NoDelay = true
        };

        try
        {
            await socket.ConnectAsync(
                allowed,
                context.DnsEndPoint.Port,
                cancellationToken);

            return new NetworkStream(socket, ownsSocket: true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }

    public static bool IsBlocked(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();

            return bytes[0] switch
            {
                0 => true,                                       // "this" network
                10 => true,                                      // private
                100 when bytes[1] is >= 64 and <= 127 => true,   // carrier-grade NAT
                127 => true,                                     // loopback
                169 when bytes[1] == 254 => true,                // link-local, cloud metadata
                172 when bytes[1] is >= 16 and <= 31 => true,    // private
                192 when bytes[1] == 168 => true,                // private
                192 when bytes[1] == 0 && bytes[2] == 0 => true, // IETF protocol assignments
                198 when bytes[1] is 18 or 19 => true,           // benchmarking
                >= 224 => true,                                  // multicast and reserved
                _ => false
            };
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var bytes = address.GetAddressBytes();

            return address.Equals(IPAddress.IPv6Any)
                || address.IsIPv6LinkLocal
                || address.IsIPv6SiteLocal
                || address.IsIPv6Multicast
                || (bytes[0] & 0xFE) == 0xFC; // unique local fc00::/7
        }

        return true;
    }
}
