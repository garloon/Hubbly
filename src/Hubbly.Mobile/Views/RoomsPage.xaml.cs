using Hubbly.Domain.Dtos.Rooms;
using Hubbly.Mobile.Models;
using Hubbly.Mobile.Services;

namespace Hubbly.Mobile.Views;

public partial class RoomsPage : ContentPage
{
    private readonly IChatApiService _chatApiService;

    public RoomsPage()
    {
        InitializeComponent();
        _chatApiService = MauiApplication.Current.Services.GetService<IChatApiService>();
        LoadRooms();
    }

    private async void LoadRooms()
    {
        try
        {
            LoadingIndicator.IsRunning = true;
            ErrorLabel.IsVisible = false;

            // Теперь RoomDto из Domain
            var rooms = await _chatApiService.GetPublicRoomsAsync();

            RoomsCollection.ItemsSource = rooms;

            if (rooms.Count == 0)
            {
                ErrorLabel.Text = "Нет доступных комнат";
                ErrorLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = $"Ошибка: {ex.Message}";
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
        }
    }

    private void OnRefreshClicked(object sender, EventArgs e)
    {
        LoadRooms();
    }

    private void OnCreateRoomClicked(object sender, EventArgs e)
    {
        DisplayAlert("Инфо", "Функция создания комнаты будет добавлена", "OK");
    }

    private async void OnRoomSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is RoomDto room)
        {
            await DisplayAlert("Выбрана комната", room.Title, "OK");
        }
    }
}