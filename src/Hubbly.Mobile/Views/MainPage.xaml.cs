using Hubbly.Mobile.Services;

namespace Hubbly.Mobile.Views;

public partial class MainPage : ContentPage
{
    private readonly ITokenStorage _tokenStorage;

    public MainPage()
    {
        InitializeComponent();
        _tokenStorage = MauiApplication.Current.Services.GetService<ITokenStorage>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CheckToken();
    }

    private async Task CheckToken()
    {
        var token = await _tokenStorage.GetTokenAsync();

        if (string.IsNullOrEmpty(token))
        {
            await DisplayAlert("Внимание", "Вы не авторизованы", "OK");
            await Navigation.PushAsync(new LoginPage());
        }
        else
        {
            await DisplayAlert("Привет!", "Вы авторизованы!", "OK");
        }
    }

    private void OnTestClicked(object sender, EventArgs e)
    {
        DisplayAlert("Тест", "MAUI приложение работает!", "OK");
    }

    private async void OnGoToLoginClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LoginPage());
    }

    private async void OnRoomsClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RoomsPage());
    }

    private async void OnTestTokenClicked(object sender, EventArgs e)
    {
        try
        {
            var tokenStorage = MauiApplication.Current.Services.GetService<ITokenStorage>();
            var token = await tokenStorage.GetTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                await DisplayAlert("Ошибка", "Токен не найден", "OK");
                return;
            }

            // Тестируем токен
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"http://192.168.1.203:5081/api/auth/test-token");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Успех!", $"Токен работает!\n{content}", "OK");
            }
            else
            {
                await DisplayAlert("Ошибка", $"Токен не работает: {response.StatusCode}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", $"Ошибка: {ex.Message}", "OK");
        }
    }
}