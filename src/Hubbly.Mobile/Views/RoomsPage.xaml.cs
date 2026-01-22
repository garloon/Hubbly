using Microsoft.Maui.Controls;

namespace Hubbly.Mobile.Views;

public partial class RoomsPage : ContentPage
{
    public RoomsPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}