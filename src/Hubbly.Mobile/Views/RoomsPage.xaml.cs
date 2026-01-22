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

    private async void OnRoomTapped(object sender, TappedEventArgs e)
    {
        try
        {

            // Способ 1: Из параметра
            if (e.Parameter is RoomDto room)
            {
                await HandleRoomSelection(room);
                return;
            }

            // Способ 2: Из BindingContext
            if (sender is BindableObject bindable && bindable.BindingContext is RoomDto roomFromContext)
            {
                await HandleRoomSelection(roomFromContext);
                return;
            }

        }
        catch (Exception ex)
        {
            await DisplayAlert("Ошибка", "Не удалось выбрать комнату", "OK");
        }
    }

    private async Task HandleRoomSelection(RoomDto room)
    {

        // Показываем информацию
        await DisplayAlert(room.Title,
            $"Описание: {room.Description ?? "нет"}\n" +
            $"Участников: {room.MemberCount}\n" +
            $"Создатель: {room.CreatorName}\n" +
            $"Вы участник: {(room.IsMember ? "Да" : "Нет")}",
            "OK");

        // TODO: Позже добавим переход в чат
        // await Navigation.PushAsync(new ChatRoomPage(room.Id, room.Title));
    }

    private async void OnRoomSelected(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            Console.WriteLine("=== OnRoomSelected called ===");

            // Снимаем выделение сразу
            RoomsCollection.SelectedItem = null;

            // Берем из PreviousSelection (может быть более надежно)
            var selected = e.PreviousSelection?.FirstOrDefault() ?? e.CurrentSelection?.FirstOrDefault();

            if (selected is RoomDto room)
            {
                Console.WriteLine($"Room selected via SelectionChanged: {room.Title}");
                await HandleRoomSelection(room);
            }
            else
            {
                Console.WriteLine("Selected item is not RoomDto or null");

                // Отладочная информация
                if (selected != null)
                {
                    Console.WriteLine($"Selected type: {selected.GetType().Name}");
                    Console.WriteLine($"Selected value: {selected}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in OnRoomSelected: {ex}");
            await DisplayAlert("Ошибка", "Не удалось выбрать комнату", "OK");
        }
    }
}