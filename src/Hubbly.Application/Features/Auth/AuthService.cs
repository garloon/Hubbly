using Hubbly.Domain.Dtos.Auth;
using Hubbly.Domain.Entities;
using Hubbly.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Hubbly.Application.Features.Auth;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ICacheService _cacheService;

    public AuthService(
        UserManager<User> userManager,
        IEmailService emailService,
        IConfiguration configuration,
        ICacheService cacheService)
    {
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
        _cacheService = cacheService;
    }

    public async Task<AuthResponseDto> RequestLoginAsync(string email)
    {
        try
        {
            // Генерируем OTP код
            var otpCode = GenerateOtpCode();

            // Сохраняем в кэш на 10 минут
            var cacheKey = $"otp:{email}";
            await _cacheService.SetAsync(cacheKey, otpCode, TimeSpan.FromMinutes(10));

            // Отправляем email
            await _emailService.SendOtpEmailAsync(email, otpCode);

            return new AuthResponseDto { Success = true };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Error = ex.Message };
        }
    }

    public async Task<AuthResponseDto> VerifyOtpAsync(string email, string otpCode)
    {
        try
        {
            // Проверяем OTP код
            var cacheKey = $"otp:{email}";
            var cachedOtp = await _cacheService.GetAsync<string>(cacheKey);

            if (cachedOtp == null || cachedOtp != otpCode)
            {
                return new AuthResponseDto { Success = false, Error = "Неверный или устаревший код" };
            }

            // Удаляем использованный OTP
            await _cacheService.RemoveAsync(cacheKey);

            // Находим или создаем пользователя
            var user = await _userManager.FindByEmailAsync(email);
            var isNewUser = false;

            if (user == null)
            {
                // Создаем нового пользователя
                user = new User
                {
                    UserName = email,
                    Email = email,
                    DisplayName = GenerateDisplayName(email),
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    return new AuthResponseDto
                    {
                        Success = false,
                        Error = string.Join(", ", result.Errors.Select(e => e.Description))
                    };
                }

                isNewUser = true;

                // TODO: Добавить пользователя в комнату для новичков
                // await AddUserToNoviceRoom(user.Id);
            }

            // Генерируем JWT токен
            var token = await GenerateJwtTokenAsync(user);

            return new AuthResponseDto
            {
                Success = true,
                Token = token,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    DisplayName = user.DisplayName,
                    AvatarUrl = user.AvatarUrl
                }
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { Success = false, Error = ex.Message };
        }
    }

    public async Task<string> GenerateJwtTokenAsync(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        // Добавляем роли если есть
        var roles = await _userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured")));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(string token, string refreshToken)
    {
        // Здесь можно добавить логику refresh токенов
        // Пока просто возвращаем тот же токен
        return await Task.FromResult(new AuthResponseDto { Success = false, Error = "Not implemented" });
    }

    private string GenerateOtpCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString(); // 6-значный код
    }

    private string GenerateDisplayName(string email)
    {
        var username = email.Split('@')[0];
        var random = new Random();
        return $"{username}_{random.Next(1000, 9999)}";
    }
}