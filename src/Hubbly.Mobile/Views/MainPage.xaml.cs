using Hubbly.Mobile.Services;
using Hubbly.Mobile.Views;

namespace Hubbly.Mobile.Views;

public partial class MainPage : ContentPage
{
    private readonly ITokenStorage _tokenStorage;
    private readonly IUserStateService _userStateService;
    private readonly IChatApiService _chatApiService;

    public MainPage()
    {
        InitializeComponent();
        _tokenStorage = MauiApplication.Current.Services.GetService<ITokenStorage>();
        _userStateService = MauiApplication.Current.Services.GetService<IUserStateService>();
        _chatApiService = MauiApplication.Current.Services.GetService<IChatApiService>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CheckAuthenticationAndRedirect();
    }

    private async Task CheckAuthenticationAndRedirect()
    {
        var isAuthenticated = await _tokenStorage.IsAuthenticatedAsync();

        if (!isAuthenticated)
        {
            await HandleNotAuthenticated();
        }
        else
        {
            await HandleAuthenticated();
        }
    }

    private async Task HandleNotAuthenticated()
    {
        var result = await DisplayAlert("Требуется вход",
            "Войдите в систему", "Войти", "Позже");

        if (result)
        {
            await Navigation.PushAsync(new LoginPage());
        }
    }

    private async Task HandleAuthenticated()
    {
        // Пробуем получить последнюю комнату
        var lastRoomId = await _userStateService.GetLastRoomIdAsync();
        var lastRoomTitle = await _userStateService.GetLastRoomTitleAsync();

        if (lastRoomId.HasValue && !string.IsNullOrEmpty(lastRoomTitle))
        {
            // Автоматический переход в последнюю комнату
            await RedirectToLastRoom(lastRoomId.Value, lastRoomTitle);
        }
        else
        {
            // Нет последней комнаты - предлагаем комнату новичков
            await OfferNoviceRoom();
        }
    }

    private async Task RedirectToLastRoom(Guid roomId, string roomTitle)
    {
        try
        {
            // Проверяем что комната все еще существует и доступна
            var room = await _chatApiService.GetRoomAsync(roomId);

            if (room != null)
            {
                // Тихий переход без уведомления
                await Navigation.PushAsync(new ChatRoomPage(roomId, roomTitle));

                // Убираем MainPage из стека навигации чтобы не вернуться
                await Task.Delay(100); // Небольшая задержка
                Navigation.RemovePage(this);
            }
            else
            {
                // Комната не найдена - предлагаем комнату новичков
                await OfferNoviceRoom();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking room: {ex.Message}");
            // При ошибке - предлагаем комнату новичков
            await OfferNoviceRoom();
        }
    }

    private async Task OfferNoviceRoom()
    {
        // Даем пользователю выбор
        await DisplayAlert("Добро пожаловать!",
            "Вы авторизованы. Используйте кнопки ниже для навигации.", "OK");
    }

    private async Task NavigateToNoviceRoom()
    {
        try
        {
            var noviceRoom = await _chatApiService.GetNoviceRoomAsync();

            if (noviceRoom != null)
            {
                await Navigation.PushAsync(new ChatRoomPage(noviceRoom.Id, noviceRoom.Title));
                await Task.Delay(100);
                Navigation.RemovePage(this);
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Не удалось перейти в чат: {ex.Message}", "OK");
        }
    }

    private async void OnTestClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Тест", "Приложение работает!", "OK");
    }

    private async void OnGoToLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage());
    }

    private async void OnRoomsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RoomsPage());
    }

    private async void OnGoToNoviceRoomClicked(object sender, EventArgs e)
    {
        await NavigateToNoviceRoom();
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        var confirm = await DisplayAlert("Выход",
            "Вы уверены что хотите выйти?", "Да, выйти", "Отмена");

        if (confirm)
        {
            await _tokenStorage.DeleteTokenAsync();
            await _userStateService.ClearLastRoomIdAsync();

            await DisplayAlert("Выход", "Вы успешно вышли", "OK");

            // Перезапускаем навигацию
            Microsoft.Maui.Controls.Application.Current.MainPage = new NavigationPage(new MainPage());
        }
    }
}