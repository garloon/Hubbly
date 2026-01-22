using Hubbly.Mobile.Views;

namespace Hubbly.Mobile;

public partial class App : Microsoft.Maui.Controls.Application
{
    public App()
    {
        InitializeComponent();

        // Стартовая страница
        MainPage = new NavigationPage(new WelcomePage());
    }
}
