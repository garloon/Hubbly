using Hubbly.Mobile.Views;
using Microsoft.Maui.Controls;

namespace Hubbly.Mobile.Services;

public interface INavigationService
{
    Task NavigateToLoginAsync();
    Task NavigateToMainAsync();
    Task NavigateToVerifyOtpAsync(string email);
    Task GoBackAsync();
    Task NavigateBackToMainAsync();
}

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task NavigateToLoginAsync()
    {
        var loginPage = _serviceProvider.GetService<LoginPage>();
        await NavigateToPageAsync(loginPage ?? new LoginPage());
    }

    public async Task NavigateToMainAsync()
    {
        var mainPage = _serviceProvider.GetService<MainPage>();
        await NavigateToPageAsync(mainPage ?? new MainPage());
    }

    public async Task NavigateToVerifyOtpAsync(string email)
    {
        var verifyPage = new VerifyOtpPage(email);
        await NavigateToPageAsync(verifyPage);
    }

    public async Task NavigateBackToMainAsync()
    {
        await Shell.Current.GoToAsync("//MainPage");
    }

    public async Task GoBackAsync()
    {
        try
        {
            if (Shell.Current?.Navigation is not null && Shell.Current.Navigation.NavigationStack.Count > 1)
            {
                await Shell.Current.Navigation.PopAsync();
            }
            else if (Microsoft.Maui.Controls.Application.Current?.MainPage is NavigationPage navigationPage && navigationPage.Navigation.NavigationStack.Count > 1)
            {
                await navigationPage.Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoBack error: {ex.Message}");
        }
    }

    private async Task NavigateToPageAsync(Page page)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // Если текущая MainPage - это NavigationPage
                    if (Microsoft.Maui.Controls.Application.Current?.MainPage is NavigationPage currentNavPage)
                    {
                        await currentNavPage.Navigation.PushAsync(page);
                    }
                    // Если это Shell
                    else if (Shell.Current is not null)
                    {
                        await Shell.Current.Navigation.PushAsync(page);
                    }
                    // Иначе создаем новую NavigationPage
                    else
                    {
                        Microsoft.Maui.Controls.Application.Current.MainPage = new NavigationPage(page);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Navigation error: {ex.Message}");
                    Microsoft.Maui.Controls.Application.Current.MainPage = new NavigationPage(page);
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Navigation error: {ex.Message}");
            Microsoft.Maui.Controls.Application.Current.MainPage = new NavigationPage(page);
        }
    }
}