namespace Hubbly.Mobile.Services;

public interface ITokenStorage
{
    Task<string?> GetTokenAsync();
    Task SaveTokenAsync(string token);
    Task DeleteTokenAsync();
    Task<bool> IsAuthenticatedAsync();
}

public class SecureStorageTokenService : ITokenStorage
{
    private const string TokenKey = "auth_token";


    public async Task<string?> GetTokenAsync()
    {
        return await SecureStorage.Default.GetAsync(TokenKey);
    }

    public async Task SaveTokenAsync(string token)
    {
        await SecureStorage.Default.SetAsync(TokenKey, token);
    }

    public async Task DeleteTokenAsync()
    {
        SecureStorage.Default.Remove(TokenKey);
        await Task.CompletedTask;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }
}