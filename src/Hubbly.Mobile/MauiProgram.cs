using Hubbly.Mobile.Services;
using Hubbly.Mobile.Utils;
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

        // Основной HTTP клиент с обработчиком авторизации
        builder.Services.AddSingleton(sp => new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Сервисы хранилища
        builder.Services.AddSingleton<ITokenStorage, SecureStorageTokenService>();
        builder.Services.AddTransient<AuthenticatedHttpClientHandler>();

        // Регистрация API сервисов через Refit
        builder.Services.AddRefitClient<IUsersApiService>()
            .ConfigureHttpClient((sp, client) =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

        builder.Services.AddRefitClient<IRoomsApiService>()
            .ConfigureHttpClient((sp, client) =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

        builder.Services.AddRefitClient<IMessagesApiService>()
            .ConfigureHttpClient((sp, client) =>
            {
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

        // Основной сервис
        builder.Services.AddSingleton<IApiService, ApiService>();

        // SignalR сервис
        builder.Services.AddSingleton<IChatHubService, ChatHubService>();

        // Сервисы приложения
        builder.Services.AddSingleton<IUserStateService, UserStateService>();
        builder.Services.AddSingleton<INavigationService, NavigationService>();

        // Страницы
        builder.Services.AddTransient<WelcomePage>();
        builder.Services.AddTransient<ChatRoomPage>();
        builder.Services.AddTransient<RoomsPage>();
        builder.Services.AddTransient<ProfilePage>();

        return builder.Build();
    }

    private static string GetBaseUrl()
    {
#if DEBUG
        // Для отладки используем IP компьютера
        return GetDebugBaseUrl();
#else
    // Для продакшена
    return "https://ваш-сервер.com";
#endif
    }

    private static string GetDebugBaseUrl()
    {
        return "http://192.168.1.203:5081";
    }
}