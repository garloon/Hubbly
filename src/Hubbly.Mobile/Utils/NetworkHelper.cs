using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Hubbly.Mobile.Utils;

public static class NetworkHelper
{
    public static string GetLocalIpAddress()
    {
        var ipAddresses = new List<string>();

        // Получаем все сетевые интерфейсы
        foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
        {
            // Пропускаем выключенные интерфейсы
            if (netInterface.OperationalStatus != OperationalStatus.Up)
                continue;

            // Получаем IP-адреса интерфейса
            var ipProperties = netInterface.GetIPProperties();

            foreach (var ipAddress in ipProperties.UnicastAddresses)
            {
                // Берем только IPv4 адреса
                if (ipAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                {
                    var ip = ipAddress.Address.ToString();

                    // Игнорируем локальные адреса
                    if (!ip.StartsWith("127.") &&
                        !ip.StartsWith("169.254.") &&
                        !ip.StartsWith("192.0.0."))
                    {
                        ipAddresses.Add(ip);
                    }
                }
            }
        }

        return ipAddresses.FirstOrDefault() ?? "localhost";
    }
}
