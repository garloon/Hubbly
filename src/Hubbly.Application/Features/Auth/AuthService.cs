using Hubbly.Domain.Dtos.Auth;
using Hubbly.Domain.Entities;
using Hubbly.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AuthService> _logger; // Добавили логгер

    public AuthService(
        UserManager<User> userManager,
        IEmailService emailService,
        IConfiguration configuration,
        ICacheService cacheService,
        IApplicationDbContext context,
        ILogger<AuthService> logger) // Добавили в конструктор
    {
        _userManager = userManager;
        _emailService = emailService;
        _configuration = configuration;
        _cacheService = cacheService;
        _context = context;
        _logger = logger;
    }

    public async Task<AuthResponseDto> RequestLoginAsync(string email)
    {
        try
        {
            _logger.LogInformation("RequestLoginAsync called for email: {Email}", email);

            // Генерируем OTP код
            var otpCode = GenerateOtpCode();
            _logger.LogInformation("Generated OTP: {OtpCode} for email: {Email}", otpCode, email);

            // Сохраняем в кэш на 10 минут
            var cacheKey = $"otp:{email}";
            _logger.LogInformation("Saving to cache with key: {CacheKey}", cacheKey);

            await _cacheService.SetAsync(cacheKey, otpCode, TimeSpan.FromMinutes(10));

            // Отправляем email
            await _emailService.SendOtpEmailAsync(email, otpCode);

            _logger.LogInformation("OTP email sent successfully to: {Email}", email);

            return new AuthResponseDto { Success = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RequestLoginAsync for email: {Email}", email);
            return new AuthResponseDto { Success = false, Error = ex.Message };
        }
    }

    public async Task<AuthResponseDto> VerifyOtpAsync(string email, string otpCode)
    {
        bool isNewUser = false;

        try
        {
            _logger.LogInformation("VerifyOtpAsync called for email: {Email}, OTP: {OtpCode}", email, otpCode);

            // Проверяем OTP код
            var cacheKey = $"otp:{email}";
            _logger.LogInformation("Checking cache with key: {CacheKey}", cacheKey);

            var cachedOtp = await _cacheService.GetAsync<string>(cacheKey);

            _logger.LogInformation("Cached OTP: {CachedOtp}, Provided OTP: {ProvidedOtp}",
                cachedOtp ?? "NULL", otpCode);

            if (cachedOtp == null || cachedOtp != otpCode)
            {
                _logger.LogWarning("OTP verification failed for email: {Email}. Cache: {Cached}, Provided: {Provided}",
                    email, cachedOtp, otpCode);
                return new AuthResponseDto { Success = false, Error = "Неверный или устаревший код" };
            }

            _logger.LogInformation("OTP verified successfully for email: {Email}", email);

            // Удаляем использованный OTP
            await _cacheService.RemoveAsync(cacheKey);

            // Находим или создаем пользователя
            var user = await _userManager.FindByEmailAsync(email);

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

                // ✅ ДОБАВЛЯЕМ ПОЛЬЗОВАТЕЛЯ В КОМНАТУ ДЛЯ НОВИЧКОВ
                await AddUserToNoviceRoom(user.Id);
            }
            else
            {
                _logger.LogInformation("Existing user found with ID: {UserId}", user.Id);
            }

            // Генерируем JWT токен
            var token = await GenerateJwtTokenAsync(user);
            _logger.LogInformation("JWT token generated for user: {UserId}", user.Id);

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
            _logger.LogError(ex, "Error in VerifyOtpAsync for email: {Email}", email);
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
        // Используем криптографически безопасный генератор
        var bytes = new byte[4];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        var randomNumber = BitConverter.ToInt32(bytes, 0) & 0x7FFFFFFF; // Только положительные числа
        return (100000 + (randomNumber % 900000)).ToString(); // 6-значный код от 100000 до 999999
    }

    private string GenerateDisplayName(string email)
    {
        var username = email.Split('@')[0];
        var random = new Random();
        return $"{username}_{random.Next(1000, 9999)}";
    }

    private async Task AddUserToNoviceRoom(Guid userId)
    {
        try
        {
            // Находим системную комнату для новичков
            var noviceRoom = await _context.ChatRooms
                .FirstOrDefaultAsync(r => r.Type == Domain.Enums.RoomType.SystemNovice);

            if (noviceRoom != null)
            {
                // Проверяем, не добавлен ли уже пользователь
                var existingMember = await _context.RoomMembers
                    .FirstOrDefaultAsync(rm => rm.RoomId == noviceRoom.Id && rm.UserId == userId);

                if (existingMember == null)
                {
                    // Добавляем пользователя в комнату
                    var roomMember = new RoomMember
                    {
                        UserId = userId,
                        RoomId = noviceRoom.Id,
                        IsAdmin = false
                    };

                    _context.RoomMembers.Add(roomMember);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("User {UserId} added to novice room {RoomId}", userId, noviceRoom.Id);
                }
            }
            else
            {
                _logger.LogWarning("Novice room not found!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user to novice room");
        }
    }
}