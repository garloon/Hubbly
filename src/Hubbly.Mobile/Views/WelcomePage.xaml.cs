using Hubbly.Mobile.Models;
using Hubbly.Mobile.Services;
using Hubbly.Mobile.Utils;
using System.Text;

namespace Hubbly.Mobile.Views;

public partial class WelcomePage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly ITokenStorage _tokenStorage;
    private readonly INavigationService _navigationService;

    public WelcomePage()
    {
        InitializeComponent();

        _apiService = MauiApplication.Current.Services.GetService<IApiService>();
        _tokenStorage = MauiApplication.Current.Services.GetService<ITokenStorage>();
        _navigationService = MauiApplication.Current.Services.GetService<INavigationService>();
    }

    private void OnNicknameChanged(object sender, TextChangedEventArgs e)
    {
        var nickname = e.NewTextValue?.Trim() ?? "";

        // Валидация никнейма
        if (nickname.Length < 3)
        {
            NicknameErrorLabel.Text = "Минимум 3 символа";
            NicknameErrorLabel.IsVisible = true;
            ContinueButton.IsEnabled = false;
        }
        else if (nickname.Length > 20)
        {
            NicknameErrorLabel.Text = "Максимум 20 символов";
            NicknameErrorLabel.IsVisible = true;
            ContinueButton.IsEnabled = false;
        }
        else
        {
            NicknameErrorLabel.IsVisible = false;
            ContinueButton.IsEnabled = true;
        }
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        var nickname = NicknameEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(nickname) || nickname.Length < 3)
        {
            await DisplayAlert("Ошибка", "Введите имя (минимум 3 символа)", "OK");
            return;
        }

        await CreateGuestUser(nickname);
    }

    private async Task CreateGuestUser(string nickname)
    {
        DebugLogger.Log($"Начало создания гостя: {nickname}");

        try
        {
            LoadingIndicator.IsVisible = true;
            ContinueButton.IsEnabled = false;
            NicknameEntry.IsEnabled = false;

            // Генерируем deviceId
            var deviceId = await GetOrCreateDeviceId();
            DebugLogger.Log($"DeviceId: {deviceId}");

            // Определяем базовый URL
            string baseUrl;

            if (DeviceInfo.Platform == DevicePlatform.Android && DeviceInfo.DeviceType == DeviceType.Virtual)
            {
                baseUrl = "http://10.0.2.2:5081";
                DebugLogger.Log($"Используем URL для Android эмулятора: {baseUrl}");
            }
            else
            {
                baseUrl = "http://192.168.1.203:5081"; // Ваш IP
                DebugLogger.Log($"Используем URL для физического устройства: {baseUrl}");
            }

            // Проверяем API
            DebugLogger.Log($"Проверяем доступность API: {baseUrl}");

            using var testClient = new HttpClient();
            testClient.Timeout = TimeSpan.FromSeconds(3);

            try
            {
                var testResponse = await testClient.GetAsync($"{baseUrl}/health");
                DebugLogger.Log($"API доступен: {testResponse.StatusCode}");

                if (!testResponse.IsSuccessStatusCode)
                {
                    await DisplayAlert("Ошибка", $"API сервер ответил с ошибкой: {testResponse.StatusCode}", "OK");
                    return;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"API недоступен: {ex.Message}");
                await DisplayAlert("Ошибка",
                    $"API сервер недоступен по адресу {baseUrl}\n\n" +
                    $"Проверьте:\n" +
                    $"1. Сервер запущен\n" +
                    $"2. Порт 5081 открыт\n" +
                    $"3. Брандмауэр разрешает соединения\n" +
                    $"4. IP адрес правильный", "OK");
                return;
            }

            // Создаем гостя (передаем baseUrl в сервис или используем напрямую)
            DebugLogger.Log("Вызываем GetOrCreateGuestAsync...");

            // Временно используем прямой вызов
            var user = await CreateGuestDirectAsync(baseUrl, deviceId, nickname);

            if (user == null)
            {
                DebugLogger.Log("GetOrCreateGuestAsync вернул null");
                await DisplayAlert("Ошибка", "Не удалось создать пользователя", "OK");
                return;
            }

            DebugLogger.Log($"Пользователь создан: {user.Id}, Ник: {user.Nickname}");

            // Находим комнату
            DebugLogger.Log("Ищем свободную комнату...");
            var room = await _apiService.GetAvailableSystemRoomAsync();

            if (room == null)
            {
                DebugLogger.Log("Не найдена свободная комната");
                await DisplayAlert("Ошибка", "Не удалось найти доступную комнату", "OK");
                return;
            }

            DebugLogger.Log($"Найдена комната: {room.Name} (ID: {room.Id})");

            // Присоединяемся
            DebugLogger.Log("Присоединяемся к комнате...");
            var joined = await _apiService.JoinRoomAsync(room.Id);

            if (!joined)
            {
                DebugLogger.Log("Не удалось присоединиться к комнате");
                await DisplayAlert("Ошибка", "Не удалось присоединиться к комнате", "OK");
                return;
            }

            DebugLogger.Log($"Успешно присоединились к комнате {room.Id}");

            // Сохраняем и переходим
            await _tokenStorage.SaveLastRoomIdAsync(room.Id);
            DebugLogger.Log($"Сохранена последняя комната: {room.Id}");

            DebugLogger.Log($"Переходим в чат: {room.Name}");
            await _navigationService.NavigateToChatRoomAsync(room.Id, room.Name);
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"ОШИБКА: {ex.Message}");
            await DisplayAlert("Ошибка", $"Не удалось войти: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsVisible = false;
            ContinueButton.IsEnabled = true;
            NicknameEntry.IsEnabled = true;
            DebugLogger.Log("CreateGuestUser завершен");
        }
    }

    private async Task<UserDto?> CreateGuestDirectAsync(string baseUrl, string deviceId, string nickname)
    {
        try
        {
            var url = $"{baseUrl}/api/users/guest";
            var request = new CreateGuestRequest
            {
                DeviceId = deviceId,
                Nickname = nickname
            };

            var json = System.Text.Json.JsonSerializer.Serialize(request);
            DebugLogger.Log($"Отправляем запрос на: {url}");
            DebugLogger.Log($"JSON: {json}");

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(url, content);

            DebugLogger.Log($"Статус ответа: {response.StatusCode}");
            var responseBody = await response.Content.ReadAsStringAsync();
            DebugLogger.Log($"Тело ответа: {responseBody}");

            if (response.IsSuccessStatusCode)
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var result = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<UserDto>>(responseBody, options);

                if (result?.Success == true && result.Data != null)
                {
                    DebugLogger.Log($"Пользователь создан: {result.Data.Id}");

                    await _tokenStorage.SaveUserIdAsync(result.Data.Id);
                    await _tokenStorage.SaveDeviceIdAsync(deviceId);

                    return result.Data;
                }
                else
                {
                    await DisplayAlert("Ошибка API", $"Сервер вернул ошибку: {result?.Error}", "OK");
                    return null;
                }
            }
            else
            {
                await DisplayAlert("HTTP Ошибка", $"Статус: {response.StatusCode}\nОтвет: {responseBody}", "OK");
                return null;
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"Ошибка при создании пользователя: {ex.Message}");
            await DisplayAlert("Сетевая ошибка", $"Не удалось отправить запрос: {ex.Message}", "OK");
            return null;
        }
    }

    private async Task<string> GetOrCreateDeviceId()
    {
        // Пробуем получить существующий deviceId
        var existingDeviceId = await _tokenStorage.GetDeviceIdAsync();

        if (!string.IsNullOrEmpty(existingDeviceId))
            return existingDeviceId;

        // Генерируем новый
        var newDeviceId = $"device_{Guid.NewGuid():N}";
        await _tokenStorage.SaveDeviceIdAsync(newDeviceId);

        return newDeviceId;
    }

    private async void OnExistingAccountClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Информация", "Пока доступен только гостевой вход. Функция регистрации появится позже.", "OK");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Проверяем, может пользователь уже авторизован
        var isAuthenticated = await _tokenStorage.IsAuthenticatedAsync();
        var lastRoomId = await _tokenStorage.GetLastRoomIdAsync();

        if (isAuthenticated && lastRoomId.HasValue)
        {
            // Автоматический переход в последнюю комнату
            await _navigationService.NavigateToChatRoomAsync(lastRoomId.Value, "Чат");
        }
    }

    private async void OnDebugClicked(object sender, EventArgs e)
    {
        try
        {
            DebugLogger.Log("=== DEBUG BUTTON CLICKED ===");

            var sb = new StringBuilder();

            // Показываем информацию об устройстве
            sb.AppendLine("=== ИНФОРМАЦИЯ ОБ УСТРОЙСТВЕ ===");
            sb.AppendLine($"Платформа: {DeviceInfo.Platform}");
            sb.AppendLine($"Тип: {DeviceInfo.DeviceType}");
            sb.AppendLine($"Модель: {DeviceInfo.Model}");
            sb.AppendLine($"Версия: {DeviceInfo.VersionString}");

            // Пробуем разные адреса
            var testUrls = new[]
            {
            "http://localhost:5081/health",
            "http://10.0.2.2:5081/health", // Android эмулятор
            "http://192.168.1.203:5081/health" // Ваш IP
        };

            sb.AppendLine("\n=== ПРОВЕРКА ДОСТУПНОСТИ API ===");

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(3);

            foreach (var url in testUrls)
            {
                try
                {
                    sb.AppendLine($"\nПробуем {url}");
                    var response = await client.GetAsync(url);
                    sb.AppendLine($"✓ Доступен: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        sb.AppendLine($"Ответ: {content}");
                    }
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"✗ Ошибка: {ex.Message}");
                }
            }

            // Данные хранилища
            sb.AppendLine("\n=== ДАННЫЕ ХРАНИЛИЩА ===");
            sb.AppendLine($"DeviceId: {await _tokenStorage.GetDeviceIdAsync() ?? "null"}");
            sb.AppendLine($"UserId: {_tokenStorage.GetUserIdAsync()?.ToString() ?? "null"}");

            // Логи
            sb.AppendLine("\n=== ЛОГИ ===");
            sb.AppendLine(DebugLogger.GetLogs());

            await DisplayAlert("Отладка", sb.ToString(), "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка отладки", ex.Message, "OK");
        }
    }
}