using Hubbly.Mobile.Views;

namespace Hubbly.Mobile.Services;

public class NavigationService : INavigationService
{
    public async Task NavigateToWelcomeAsync()
    {
        await NavigateToPageAsync(new WelcomePage());
    }

    public async Task NavigateToChatRoomAsync(Guid roomId, string roomTitle)
    {
        await NavigateToPageAsync(new ChatRoomPage(roomId, roomTitle));
    }

    public async Task NavigateToProfileAsync()
    {
        //await NavigateToPageAsync(new ProfilePage());
    }

    public async Task NavigateToRoomsAsync()
    {
        await NavigateToPageAsync(new RoomsPage());
    }

    public async Task GoBackAsync()
    {
        try
        {
            if (Microsoft.Maui.Controls.Application.Current?.MainPage is NavigationPage navigationPage)
            {
                await navigationPage.Navigation.PopAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GoBack error: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        // Очищаем стек навигации и переходим на Welcome
        if (Microsoft.Maui.Controls.Application.Current?.MainPage is NavigationPage navigationPage)
        {
            navigationPage.Navigation.RemovePage(navigationPage.CurrentPage);
            await NavigateToWelcomeAsync();
        }
    }

    private async Task NavigateToPageAsync(Page page)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                if (Microsoft.Maui.Controls.Application.Current?.MainPage is NavigationPage currentNavPage)
                {
                    await currentNavPage.Navigation.PushAsync(page);
                }
                else
                {
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
