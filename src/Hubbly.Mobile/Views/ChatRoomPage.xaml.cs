using Hubbly.Mobile.Models.Shared;
using Hubbly.Mobile.Services;

namespace Hubbly.Mobile.Views;

public partial class ChatRoomPage : ContentPage
{
    private readonly IChatApiService _chatApiService;
    private readonly IUserStateService _userStateService;
    private readonly Guid _roomId;
    private readonly string _roomTitle;

    public ChatRoomPage(Guid roomId, string roomTitle)
    {
        InitializeComponent();

        _roomId = roomId;
        _roomTitle = roomTitle;
        _chatApiService = MauiApplication.Current.Services.GetService<IChatApiService>();
        _userStateService = MauiApplication.Current.Services.GetService<IUserStateService>();

        RoomTitleLabel.Text = roomTitle;
        RoomInfoLabel.Text = $"ID: {roomId}";

        // Сохраняем как последнюю комнату
        SaveAsLastRoom();

        LoadMessages();
    }

    private async void SaveAsLastRoom()
    {
        try
        {
            await _userStateService.SaveLastRoomIdAsync(_roomId);
            await _userStateService.SaveLastRoomTitleAsync(_roomTitle);
            Console.WriteLine($"Saved last room: {_roomTitle} ({_roomId})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving last room: {ex}");
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        LoadMessages();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }

    private async void OnSendClicked(object sender, EventArgs e)
    {
        await SendMessage();
    }

    private async void OnMessageSendRequested(object sender, EventArgs e)
    {
        await SendMessage();
    }

    private async void LoadMessages()
    {
        try
        {
            LoadingIndicator.IsRunning = true;

            // Сначала входим в комнату
            await _chatApiService.JoinRoomAsync(_roomId);

            // Получаем сообщения как JSON строку
            var json = await _chatApiService.GetRoomMessagesJsonAsync(_roomId);
            Console.WriteLine($"Messages JSON: {json}");

            // Парсим вручную
            var messages = ParseMessagesJson(json);

            MessagesCollection.ItemsSource = messages;

            // Прокручиваем вниз
            if (messages.Any())
            {
                MessagesCollection.ScrollTo(messages.Last());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading messages: {ex}");
            await DisplayAlert("Ошибка", $"Не удалось загрузить сообщения: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
        }
    }

    private List<MessageDto> ParseMessagesJson(string json)
    {
        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var messages = System.Text.Json.JsonSerializer.Deserialize<List<MessageDto>>(json, options);
            return messages ?? new List<MessageDto>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing JSON: {ex}");
            Console.WriteLine($"JSON: {json}");
            return new List<MessageDto>();
        }
    }

    private async Task SendMessage()
    {
        var text = MessageEntry.Text?.Trim();

        if (string.IsNullOrEmpty(text))
        {
            await DisplayAlert("Ошибка", "Введите текст сообщения", "OK");
            return;
        }

        try
        {
            MessageEntry.IsEnabled = false;

            Console.WriteLine($"Sending message: {text}");

            var request = new SendMessageRequest
            {
                Text = text,
                Type = 1 // Text
            };

            // Отправляем и получаем JSON
            var responseJson = await _chatApiService.SendMessageJsonAsync(_roomId, request);
            Console.WriteLine($"Send response JSON: {responseJson}");

            // Парсим ответ
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var message = System.Text.Json.JsonSerializer.Deserialize<MessageDto>(responseJson, options);

            if (message != null)
            {
                Console.WriteLine($"Message sent: {message.Id}");
                MessageEntry.Text = string.Empty;
                LoadMessages(); // Обновляем список
            }
            else
            {
                await DisplayAlert("Ошибка", "Не удалось отправить сообщение", "OK");
            }
        }
        catch (Refit.ApiException ex)
        {
            Console.WriteLine($"API Error: {ex.StatusCode} - {ex.Content}");
            await DisplayAlert("Ошибка отправки",
                $"Ошибка {ex.StatusCode}: {ex.Content}",
                "OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception: {ex}");
            await DisplayAlert("Ошибка", $"Не удалось отправить сообщение: {ex.Message}", "OK");
        }
        finally
        {
            MessageEntry.IsEnabled = true;
            MessageEntry.Focus();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        MessageEntry.Focus();
    }
}