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

        // API сервисы через Refit
        builder.Services.AddRefitClient<IAuthApiService>()
            .ConfigureHttpClient((sp, client) =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

        // Добавим сервис для работы с API
        builder.Services.AddSingleton<IApiService, ApiService>();

        // Локальные сервисы
        builder.Services.AddSingleton<ITokenStorage, SecureStorageTokenService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        // Страницы
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<VerifyOtpPage>();

        return builder.Build();
    }

    private static string GetBaseUrl()
    {
        // ВСЕГДА используем ваш IP
        return "http://192.168.1.203:5081";
    }
}