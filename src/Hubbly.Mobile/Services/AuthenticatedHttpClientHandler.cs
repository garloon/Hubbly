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
        // Получаем токен из хранилища
        var token = await _tokenStorage.GetTokenAsync();

        // Добавляем Bearer токен в заголовок Authorization
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}