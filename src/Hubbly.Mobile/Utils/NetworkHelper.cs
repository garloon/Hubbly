namespace Hubbly.Mobile.Utils;

public static class NetworkHelper
{
    public static async Task<string> GetLocalIpAddress()
    {
        try
        {
            // Для Android можно попробовать получить IP
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Простой способ - запросить у пользователя
                return await GetIpFromUser();
            }

            return "192.168.1.100"; // Дефолтный IP
        }
        catch
        {
            return "192.168.1.100";
        }
    }

    private static async Task<string> GetIpFromUser()
    {
        // Можно сделать диалог для ввода IP
        // Пока просто возвращаем дефолтный
        return "192.168.1.100";
    }

    public static async Task<bool> TestConnection(string ip)
    {
        try
        {
            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(3),
                BaseAddress = new Uri($"http://{ip}:5081")
            };

            var response = await client.GetAsync("/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}