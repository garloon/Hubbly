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

            await DisplayAlert("Проверка", "Проверяем код...", "OK");

            Console.WriteLine($"=== MAUI Debug ===");
            Console.WriteLine($"Email: {_email}");
            Console.WriteLine($"OTP Code: {otpCode}");

            // ПРЯМОЙ ВЫЗОВ - без проверки Success
            var response = await _authApiService.VerifyOtpAsync(new VerifyOtpRequest
            {
                Email = _email,
                OtpCode = otpCode
            });

            Console.WriteLine($"Response received:");
            Console.WriteLine($"  Token: {response.Token?.Substring(0, Math.Min(20, response.Token?.Length ?? 0))}...");
            Console.WriteLine($"  User: {response.User?.Email}");

            if (!string.IsNullOrEmpty(response.Token))
            {
                await _tokenStorage.SaveTokenAsync(response.Token);

                await DisplayAlert("Успех!", "Вход выполнен!", "OK");
                await Navigation.PushAsync(new MainPage());
            }
            else
            {
                await DisplayAlert("Ошибка", "Неверный код или ошибка сервера", "OK");
            }
        }
        catch (Refit.ApiException refitEx)
        {
            Console.WriteLine($"Refit Exception: {refitEx.StatusCode} - {refitEx.Content}");
            await DisplayAlert("Ошибка", $"Ошибка API: {refitEx.StatusCode}", "OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex}");
            await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
        }
        finally
        {
            VerifyButton.IsEnabled = true;
            OtpEntry.IsEnabled = true;
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