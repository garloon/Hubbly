using System.Net.Http.Headers;

namespace Hubbly.Mobile.Services;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly ITokenStorage _tokenStorage;

    public AuthenticatedHttpClientHandler(ITokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Добавляем UserId в заголовок если есть
        var userId = await _tokenStorage.GetUserIdAsync();
        if (userId.HasValue)
        {
            request.Headers.Add("X-User-Id", userId.Value.ToString());
        }

        // Добавляем DeviceId если есть
        var deviceId = await _tokenStorage.GetDeviceIdAsync();
        if (!string.IsNullOrEmpty(deviceId))
        {
            request.Headers.Add("X-Device-Id", deviceId);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}