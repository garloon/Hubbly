using Hubbly.Mobile.Models;
using Hubbly.Mobile.Services;
using Hubbly.Mobile.Utils;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace Hubbly.Mobile.Views;

public partial class ChatRoomPage : ContentPage
{
    private readonly IApiService _apiService;
    private readonly IChatHubService _chatHubService;
    private readonly ITokenStorage _tokenStorage;
    private readonly IUserStateService _userStateService;

    private readonly Guid _roomId;
    private readonly string _roomTitle;
    private readonly ObservableCollection<MessageDto> _messages = new();

    private bool _isSidebarVisible = false;
    private bool _isConnectedToHub = false;
    private IDispatcherTimer _onlineTimer;

    public ChatRoomPage(Guid roomId, string roomTitle)
    {
        InitializeComponent();

        _roomId = roomId;
        _roomTitle = roomTitle;

        _apiService = MauiApplication.Current.Services.GetService<IApiService>();
        _chatHubService = MauiApplication.Current.Services.GetService<IChatHubService>();
        _tokenStorage = MauiApplication.Current.Services.GetService<ITokenStorage>();
        _userStateService = MauiApplication.Current.Services.GetService<IUserStateService>();

        RoomTitleLabel.Text = roomTitle;
        MessagesCollection.ItemsSource = _messages;

        InitializeChat();
    }

    private async void InitializeChat()
    {
        try
        {
            LoadingIndicator.IsRunning = true;

            // Сохраняем как последнюю комнату
            await _userStateService.SaveLastRoomIdAsync(_roomId);
            await _userStateService.SaveLastRoomTitleAsync(_roomTitle);

            // Загружаем пользователя для сайдбара
            await LoadUserInfo();

            // Подключаемся к SignalR
            await ConnectToChatHub();

            // Загружаем последние сообщения
            await LoadRecentMessages();

            // Запускаем таймер для обновления онлайн счетчика
            StartOnlineTimer();

        }
        catch (Exception ex)
        {
            DebugLogger.Log($"Error initializing chat: {ex}");
            await DisplayAlert("Ошибка", "Не удалось загрузить чат", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
        }
    }

    private async Task LoadUserInfo()
    {
        try
        {
            var user = await _apiService.GetCurrentUserAsync();
            if (user != null)
            {
                SidebarUserName.Text = user.Nickname;
                SidebarUserStatus.Text = user.IsGuest ? "гость" : "зарегистрирован";
            }
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"Error loading user info: {ex}");
        }
    }

    private void StartOnlineTimer()
    {
        // Таймер для периодического обновления онлайн счетчика
        _onlineTimer = Dispatcher.CreateTimer();
        _onlineTimer.Interval = TimeSpan.FromSeconds(30);
        _onlineTimer.Tick += async (s, e) => await RefreshOnlineCount();
        _onlineTimer.Start();

        // Первое обновление
        Task.Run(RefreshOnlineCount);
    }

    private async Task RefreshOnlineCount()
    {
        try
        {
            // Можно вызвать API для получения актуального счетчика
            // Пока будем использовать текущее значение из событий SignalR
            DebugLogger.Log($"Online timer tick. Current count: {OnlineCountLabel.Text}");
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"Error in online timer: {ex.Message}");
        }
    }

    private async Task ConnectToChatHub()
    {
        try
        {
            var deviceId = await _tokenStorage.GetDeviceIdAsync();
            var userId = await _tokenStorage.GetUserIdAsync();

            if (string.IsNullOrEmpty(deviceId) || !userId.HasValue)
            {
                await DisplayAlert("Ошибка", "Не удалось подключиться к чату", "OK");
                return;
            }

            // Подключаемся к хабу
            await _chatHubService.ConnectAsync(deviceId, userId.Value);
            _isConnectedToHub = true;

            // Присоединяемся к комнате
            await _chatHubService.JoinRoomAsync(_roomId, userId.Value);

            // Подписываемся на события
            _chatHubService.MessageReceived += OnMessageReceived;
            _chatHubService.UserJoined += OnUserJoined;
            _chatHubService.UserLeft += OnUserLeft;
            _chatHubService.UserTyping += OnUserTyping;

            DebugLogger.Log("Connected to SignalR hub");
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"Error connecting to hub: {ex}");
            _isConnectedToHub = false;
        }
    }

    private async Task LoadRecentMessages()
    {
        try
        {
            // Загружаем последние 20 сообщений
            var messages = await _apiService.GetRoomMessagesAsync(_roomId, 20);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _messages.Clear();
                foreach (var message in messages)
                {
                    _messages.Add(message);
                }

                // Прокручиваем к последнему сообщению
                if (_messages.Any())
                {
                    MessagesCollection.ScrollTo(_messages.Last(), animate: false);
                }
            });
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"Error loading recent messages: {ex}");
        }
    }

    // Обработчики событий SignalR
    private void OnMessageReceived(object sender, MessageDto message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _messages.Add(message);
            MessagesCollection.ScrollTo(message, animate: true);
        });
    }

    private void OnUserJoined(object sender, object data)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                DebugLogger.Log($"UserJoined: {data}");

                // Парсим данные для получения онлайн счетчика
                var json = JsonSerializer.Serialize(data);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("OnlineCount", out var onlineCountElement) &&
                    onlineCountElement.TryGetInt32(out var onlineCount))
                {
                    UpdateOnlineCount(onlineCount);
                    DebugLogger.Log($"User joined. Online count: {onlineCount}");
                }

                // Можно показать уведомление о входе пользователя
                if (root.TryGetProperty("UserNickname", out var nicknameElement))
                {
                    // Можно добавить временное уведомление в чат
                    DebugLogger.Log($"{nicknameElement.GetString()} вошел в комнату");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"Error processing UserJoined: {ex.Message}");
            }
        });
    }

    private void OnUserLeft(object sender, object data)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                DebugLogger.Log($"UserLeft: {data}");

                // Парсим данные для получения онлайн счетчика
                var json = JsonSerializer.Serialize(data);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("OnlineCount", out var onlineCountElement) &&
                    onlineCountElement.TryGetInt32(out var onlineCount))
                {
                    UpdateOnlineCount(onlineCount);
                    DebugLogger.Log($"User left. Online count: {onlineCount}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.Log($"Error processing UserLeft: {ex.Message}");
            }
        });
    }

    private void OnUserTyping(object sender, object data)
    {
        DebugLogger.Log($"User typing: {data}");
        // Можно добавить индикатор печати в UI
    }

    // Обновление онлайн счетчика в UI
    private void UpdateOnlineCount(int count)
    {
        OnlineCountLabel.Text = count.ToString();
        RoomInfoLabel.Text = $"{count}/100 онлайн";
    }

    // UI события
    private void OnMenuClicked(object sender, EventArgs e)
    {
        ToggleSidebar();
    }

    private async void ToggleSidebar()
    {
        _isSidebarVisible = !_isSidebarVisible;

        if (_isSidebarVisible)
        {
            Sidebar.IsVisible = true;
            await Sidebar.TranslateTo(0, 0, 250, Easing.CubicOut);
        }
        else
        {
            await Sidebar.TranslateTo(-280, 0, 250, Easing.CubicIn);
            Sidebar.IsVisible = false;
        }
    }

    private void OnMessageTextChanged(object sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue?.Trim() ?? "";
        SendButton.IsEnabled = !string.IsNullOrWhiteSpace(text);
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        await SendMessage();
    }

    private async void OnSendMessageRequested(object sender, EventArgs e)
    {
        await SendMessage();
    }

    private async Task SendMessage()
    {
        var text = MessageEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(text))
            return;

        try
        {
            MessageEntry.IsEnabled = false;
            SendButton.IsEnabled = false;

            var userId = await _tokenStorage.GetUserIdAsync();
            if (!userId.HasValue)
                throw new Exception("User not found");

            // Отправляем через SignalR
            await _chatHubService.SendMessageAsync(_roomId, text, userId.Value);

            // Очищаем поле ввода
            MessageEntry.Text = string.Empty;

            // Фокус обратно на поле ввода
            MessageEntry.Focus();
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"Error sending message: {ex}");
            await DisplayAlert("Ошибка", $"Не удалось отправить сообщение: {ex.Message}", "OK");
        }
        finally
        {
            MessageEntry.IsEnabled = true;
            SendButton.IsEnabled = false;
        }
    }

    private async void OnProfileTapped(object sender, EventArgs e)
    {
        await CloseSidebarAndNavigate(async () =>
        {
            // TODO: Добавить страницу профиля
            await DisplayAlert("Инфо", "Страница профиля в разработке", "OK");
        });
    }

    private async void OnRoomsTapped(object sender, EventArgs e)
    {
        await CloseSidebarAndNavigate(async () =>
        {
            await Navigation.PushAsync(new RoomsPage());
        });
    }

    private async void OnLogoutTapped(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Выход", "Вы уверены что хотите выйти?", "Да", "Нет");

        if (confirm)
        {
            await Logout();
        }
    }

    private async Task Logout()
    {
        try
        {
            // Отключаемся от хаба
            if (_isConnectedToHub)
            {
                var userId = await _tokenStorage.GetUserIdAsync();
                if (userId.HasValue)
                {
                    await _chatHubService.LeaveRoomAsync(_roomId, userId.Value);
                }
                await _chatHubService.DisconnectAsync();
            }

            // Очищаем данные
            await _tokenStorage.DeleteUserIdAsync();
            await _tokenStorage.DeleteDeviceIdAsync();
            await _tokenStorage.ClearLastRoomIdAsync();

            // Возвращаемся на WelcomePage
            await Navigation.PopToRootAsync();
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"Error logging out: {ex}");
            await DisplayAlert("Ошибка", "Не удалось выйти", "OK");
        }
    }

    private async Task CloseSidebarAndNavigate(Func<Task> navigationAction)
    {
        if (_isSidebarVisible)
        {
            await Sidebar.TranslateTo(-280, 0, 200, Easing.CubicIn);
            Sidebar.IsVisible = false;
            _isSidebarVisible = false;
        }

        await navigationAction();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        // Останавливаем таймер
        _onlineTimer?.Stop();
        _onlineTimer = null;

        // Отписываемся от событий
        if (_chatHubService != null)
        {
            _chatHubService.MessageReceived -= OnMessageReceived;
            _chatHubService.UserJoined -= OnUserJoined;
            _chatHubService.UserLeft -= OnUserLeft;
            _chatHubService.UserTyping -= OnUserTyping;
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MessageEntry.Focus();
    }
}