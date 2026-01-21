using Hubbly.Mobile.Models;
using Refit;

namespace Hubbly.Mobile.Services;

public interface IAuthApiService
{
    [Post("/api/auth/request-login")]
    Task<SimpleResponse> RequestLoginAsync([Body] LoginRequest request);

    [Post("/api/auth/verify-otp")]
    Task<AuthResponse> VerifyOtpAsync([Body] VerifyOtpRequest request);
}