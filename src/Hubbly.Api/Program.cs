using FluentValidation;
using FluentValidation.AspNetCore;
using Hubbly.Api.Hubs;
using Hubbly.Api.Middleware;
using Hubbly.Application.Common.Models;
using Hubbly.Application.Features.Chat;
using Hubbly.Application.Features.Rooms;
using Hubbly.Application.Features.Users;
using Hubbly.Application.Features.Users.Validators;
using Hubbly.Domain.Interfaces;
using Hubbly.Infrastructure.Data;
using Hubbly.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

builder.Services.AddHttpContextAccessor();

// Добавляем SignalR
builder.Services.AddSignalR();

// Redis Configuration
var redisConnection = builder.Configuration.GetConnectionString("Redis");
Console.WriteLine($"Redis connection: {redisConnection}");

// 1. Регистрация Redis как IDistributedCache
try
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "Hubbly_";
    });
    Console.WriteLine("Redis cache configured successfully");
}
catch (Exception ex)
{
    Console.WriteLine($"Warning: Redis configuration failed: {ex.Message}");
    Console.WriteLine("Falling back to in-memory cache");
    builder.Services.AddDistributedMemoryCache();
}

// 2. Регистрация ConnectionMultiplexer
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    try
    {
        var config = ConfigurationOptions.Parse(redisConnection);
        config.AbortOnConnectFail = false;
        config.ConnectTimeout = 5000;
        config.SyncTimeout = 5000;
        return ConnectionMultiplexer.Connect(config);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error connecting to Redis: {ex.Message}");
        throw;
    }
});

// Регистрация сервисов Application
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoomService, RoomService>(); // Регистрируем как IRoomService
builder.Services.AddScoped<RoomService>(); // И как конкретный тип для ChatHub
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<OnlineUsersService>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddScoped<IValidator<string>, NicknameValidator>();

// CORS для мобильного приложения
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowMobile", policy =>
    {
        policy.WithOrigins(
                "http://localhost",
                "http://localhost:5000",
                "http://localhost:5001",
                "http://localhost:5081",
                "http://127.0.0.1",
                "http://127.0.0.1:5081",
                "http://10.0.2.2",         // Android эмулятор
                "http://10.0.2.2:5081"
            )
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin)) return false;

                if (origin.StartsWith("http://localhost") ||
                    origin.StartsWith("https://localhost"))
                    return true;

                if (origin.StartsWith("http://127.0.0.1") ||
                    origin.StartsWith("https://127.0.0.1"))
                    return true;

                if (origin.StartsWith("http://10.0.2.2") ||
                    origin.StartsWith("https://10.0.2.2"))
                    return true;

                if (origin.StartsWith("http://192.168.1.203") ||
                    origin.StartsWith("https://192.168.1.203"))
                    return true;

                var host = new Uri(origin).Host;
                if (host.StartsWith("192.168."))
                    return true;

                if (host.StartsWith("10."))
                    return true;

                if (host.StartsWith("172."))
                {
                    var parts = host.Split('.');
                    if (parts.Length > 1 && int.TryParse(parts[1], out var secondOctet))
                    {
                        if (secondOctet >= 16 && secondOctet <= 31)
                            return true;
                    }
                }

                return false;
            })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .WithExposedHeaders("Connection", "Upgrade")
            .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрируем интерфейс DbContext
builder.Services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

// Email settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowMobile");

app.UseUserValidation();
app.UseAuthorization();

app.MapControllers();
app.UseWebSockets();

// Добавляем SignalR Hub endpoint
app.MapHub<ChatHub>("/chatHub");

// Health checks
app.MapGet("/", () => "Hubbly API is running!");
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow
}));

app.Run("http://0.0.0.0:5081");