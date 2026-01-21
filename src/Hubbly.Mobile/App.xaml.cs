using Hubbly.Mobile.Views;

namespace Hubbly.Mobile;

public partial class App : IApplication
{
    public App()
    {
        InitializeComponent();

        MainPage = new NavigationPage(new MainPage());
    }
}
