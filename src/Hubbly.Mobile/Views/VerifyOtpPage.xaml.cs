using Hubbly.Mobile.Models;
using Hubbly.Mobile.Services;

namespace Hubbly.Mobile.Views;

public partial class VerifyOtpPage : ContentPage
{
    private readonly IAuthApiService _authApiService;
    private readonly ITokenStorage _tokenStorage;
    private string _email;

    public VerifyOtpPage(string email)
    {
        InitializeComponent();

        _email = email;
        EmailLabel.Text = $"Код отправлен на: {email}";

        // Получаем сервисы
        _authApiService = MauiApplication.Current.Services.GetService<IAuthApiService>();
        _tokenStorage = MauiApplication.Current.Services.GetService<ITokenStorage>();
    }

    private async void OnVerifyClicked(object sender, EventArgs e)
    {
        var otpCode = OtpEntry.Text?.Trim();

        if (string.IsNullOrEmpty(otpCode) || otpCode.Length != 6)
        {
            await DisplayAlert("Ошибка", "Введите 6-значный код", "OK");
            return;
        }

        await VerifyOtp(otpCode);
    }

    private async Task VerifyOtp(string otpCode)
    {
        try
        {
            VerifyButton.IsEnabled = false;
            OtpEntry.IsEnabled = false;

            var response = await _authApiService.VerifyOtpAsync(new VerifyOtpRequest
            {
                Email = _email,
                OtpCode = otpCode
            });

            if (!string.IsNullOrEmpty(response.Token))
            {
                await _tokenStorage.SaveTokenAsync(response.Token);

                // Тихий вход без уведомления
                await NavigateAfterLogin();
            }
            else
            {
                await DisplayAlert("Ошибка", "Неверный код", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
        }
        finally
        {
            VerifyButton.IsEnabled = true;
            OtpEntry.IsEnabled = true;
        }
    }

    private async Task NavigateAfterLogin()
    {
        // Для новых пользователей - в комнату новичков
        // Для существующих - в последнюю комнату
        try
        {
            var chatApiService = MauiApplication.Current.Services.GetService<IChatApiService>();
            var userStateService = MauiApplication.Current.Services.GetService<IUserStateService>();

            // Пробуем получить последнюю комнату
            var lastRoomId = await userStateService.GetLastRoomIdAsync();
            var lastRoomTitle = await userStateService.GetLastRoomTitleAsync();

            if (lastRoomId.HasValue && !string.IsNullOrEmpty(lastRoomTitle))
            {
                // Переходим в последнюю комнату
                await Navigation.PushAsync(new ChatRoomPage(lastRoomId.Value, lastRoomTitle));
            }
            else
            {
                // Нет последней комнаты - идем в комнату новичков
                var noviceRoom = await chatApiService.GetNoviceRoomAsync();

                if (noviceRoom != null)
                {
                    await Navigation.PushAsync(new ChatRoomPage(noviceRoom.Id, noviceRoom.Title));
                }
                else
                {
                    await Navigation.PushAsync(new MainPage());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in NavigateAfterLogin: {ex}");
            await Navigation.PushAsync(new MainPage());
        }
    }

    private async Task NavigateToNoviceRoom()
    {
        try
        {
            var chatApiService = MauiApplication.Current.Services.GetService<IChatApiService>();

            // Получаем комнату для новичков
            var noviceRoom = await chatApiService.GetNoviceRoomAsync();

            if (noviceRoom != null)
            {
                // Переходим прямо в чат комнаты новичков
                await Navigation.PushAsync(new ChatRoomPage(noviceRoom.Id, noviceRoom.Title));
            }
            else
            {
                // Если комнаты нет, идем на главную
                await Navigation.PushAsync(new MainPage());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error navigating to novice room: {ex}");
            await Navigation.PushAsync(new MainPage());
        }
    }

    private async void OnResendClicked(object sender, EventArgs e)
    {
        await DisplayAlert("Инфо", "Функция отправки нового кода будет добавлена", "OK");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}