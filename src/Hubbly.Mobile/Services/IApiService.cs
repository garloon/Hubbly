using Hubbly.Mobile.Models;

namespace Hubbly.Mobile.Services;

public interface IApiService
{
    Task<bool> IsAuthenticatedAsync();
    Task<bool> LoginWithEmailAsync(string email);
    Task<AuthResult> VerifyOtpAsync(string email, string otpCode);
}

public class ApiService : IApiService
{
    private readonly IAuthApiService _authApiService;
    private readonly ITokenStorage _tokenStorage;

    public ApiService(IAuthApiService authApiService, ITokenStorage tokenStorage)
    {
        _authApiService = authApiService;
        _tokenStorage = tokenStorage;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _tokenStorage.GetTokenAsync();
        return !string.IsNullOrEmpty(token);
    }

    public async Task<bool> LoginWithEmailAsync(string email)
    {
        try
        {
            var response = await _authApiService.RequestLoginAsync(new LoginRequest { Email = email });
            // Теперь response - это SimpleResponse, проверяем что он не null
            return response != null;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AuthResult> VerifyOtpAsync(string email, string otpCode)
    {
        try
        {
            var response = await _authApiService.VerifyOtpAsync(new VerifyOtpRequest
            {
                Email = email,
                OtpCode = otpCode
            });

            // Теперь response - это AuthResponse напрямую
            if (response != null && !string.IsNullOrEmpty(response.Token))
            {
                await _tokenStorage.SaveTokenAsync(response.Token);
                return new AuthResult
                {
                    Success = true,
                    User = response.User
                };
            }

            return new AuthResult
            {
                Success = false,
                Error = "Неверный ответ от сервера"
            };
        }
        catch (Refit.ApiException refitEx)
        {
            // Обработка ошибок HTTP (401, 400 и т.д.)
            return new AuthResult
            {
                Success = false,
                Error = $"Ошибка API: {refitEx.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }
}