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
}