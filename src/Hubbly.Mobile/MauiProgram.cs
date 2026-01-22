using Hubbly.Mobile.Services;
using Hubbly.Mobile.Views;
using Microsoft.Extensions.Logging;
using Refit;

namespace Hubbly.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Настройка Base URL
        var baseUrl = GetBaseUrl();
        Console.WriteLine($"Using base URL: {baseUrl}");

        // Основной HTTP клиент
        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        });

        builder.Services.AddSingleton<ITokenStorage, SecureStorageTokenService>();
        builder.Services.AddTransient<AuthenticatedHttpClientHandler>();

        // API сервисы через Refit с авторизацией
        builder.Services.AddRefitClient<IAuthApiService>()
            .ConfigureHttpClient((sp, client) =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        // IChatApiService с добавлением токена
        builder.Services.AddRefitClient<IChatApiService>()
            .ConfigureHttpClient((sp, client) =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

        // Добавим сервис для работы с API
        builder.Services.AddSingleton<IApiService, ApiService>();

        builder.Services.AddSingleton<INavigationService, NavigationService>();
        builder.Services.AddSingleton<IUserStateService, UserStateService>();

        // Страницы
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<VerifyOtpPage>();
        builder.Services.AddTransient<RoomsPage>();
        builder.Services.AddTransient<ChatRoomPage>();

        return builder.Build();
    }

    private static string GetBaseUrl()
    {
        // ВСЕГДА используем ваш IP
        return "http://192.168.1.203:5081";
    }
}