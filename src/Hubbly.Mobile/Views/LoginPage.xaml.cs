using Hubbly.Mobile.Models;
using Hubbly.Mobile.Services;

namespace Hubbly.Mobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly IAuthApiService _authApiService;

    public LoginPage()
    {
        InitializeComponent();
        _authApiService = MauiApplication.Current.Services.GetService<IAuthApiService>();
    }

    private async void OnGetCodeClicked(object sender, EventArgs e)
    {
        var email = EmailEntry.Text?.Trim();

        if (string.IsNullOrEmpty(email))
        {
            await DisplayAlert("Ошибка", "Введите email", "OK");
            return;
        }

        await RequestOtp(email);
    }

    private async Task RequestOtp(string email)
    {
        try
        {
            EmailEntry.IsEnabled = false;

            await DisplayAlert("Отправка", $"Отправляем код на {email}...", "OK");

            // Теперь response - это SimpleResponse, а не ServiceResponse<SimpleResponse>
            var response = await _authApiService.RequestLoginAsync(new LoginRequest { Email = email });

            // Просто проверяем что ответ пришел
            if (response != null)
            {
                await DisplayAlert("Успешно!", response.Message ?? "Код отправлен на email", "OK");
                await Navigation.PushAsync(new VerifyOtpPage(email));
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось отправить код", "OK");
            }
        }
        catch (Refit.ApiException refitEx)
        {
            // Ошибка HTTP (400, 500 и т.д.)
            await DisplayAlert("Ошибка", $"Ошибка сервера: {refitEx.StatusCode}", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
        }
        finally
        {
            EmailEntry.IsEnabled = true;
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}