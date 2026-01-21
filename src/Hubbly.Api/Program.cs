using Hubbly.Api.Hubs;
using Hubbly.Api.Middleware;
using Hubbly.Application.Common.Models;
using Hubbly.Application.Features.Auth;
using Hubbly.Application.Features.Chat;
using Hubbly.Application.Features.Rooms;
using Hubbly.Domain.Entities;
using Hubbly.Domain.Enums;
using Hubbly.Domain.Interfaces;
using Hubbly.Infrastructure.Data;
using Hubbly.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Добавляем SignalR
builder.Services.AddSignalR();

builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IChatService, ChatService>();

// CORS для мобильного приложения
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowTestServer", policy =>
    {
        policy.WithOrigins("http://localhost:5500")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cache service - используем InMemory для разработки
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ICacheService, InMemoryCacheService>();

// Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Регистрируем интерфейс DbContext
builder.Services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

// Identity с нашим User классом
builder.Services.AddIdentity<User, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Добавляем поддержку токена из query string
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Читаем токен из query string
            var accessToken = context.Request.Query["access_token"];

            // Если есть токен в query string - используем его
            if (!string.IsNullOrEmpty(accessToken))
            {
                context.Token = accessToken;
            }
            // Иначе пытаемся прочитать из заголовка Authorization
            else
            {
                var authorizationHeader = context.Request.Headers["Authorization"].FirstOrDefault();
                if (!string.IsNullOrEmpty(authorizationHeader) &&
                    authorizationHeader.StartsWith("Bearer "))
                {
                    context.Token = authorizationHeader.Substring("Bearer ".Length).Trim();
                }
            }

            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };
});

// Email settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseCors("AllowTestServer");

app.UseAuthentication(); // Добавляем аутентификацию
app.UseCurrentUser();
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

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Создаем системную комнату для новичков если её нет
    try
    {
        var noviceRoom = await context.ChatRooms
            .FirstOrDefaultAsync(r => r.Type == RoomType.SystemNovice);

        if (noviceRoom == null)
        {
            var systemUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email == "system@hubbly.com");

            if (systemUser == null)
            {
                systemUser = new User
                {
                    UserName = "system@hubbly.com",
                    Email = "system@hubbly.com",
                    DisplayName = "Hubbly System",
                    EmailConfirmed = true
                };

                await context.Users.AddAsync(systemUser);
                await context.SaveChangesAsync();
            }

            noviceRoom = new ChatRoom
            {
                Title = "Добро пожаловать в Hubbly!",
                Description = "Комната для новых пользователей. Знакомьтесь, общайтесь, задавайте вопросы!",
                Type = RoomType.SystemNovice,
                CreatorId = systemUser.Id
            };

            await context.ChatRooms.AddAsync(noviceRoom);
            await context.SaveChangesAsync();

            logger.LogInformation("System novice room created with ID: {RoomId}", noviceRoom.Id);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error creating system novice room");
    }
}

app.Run();