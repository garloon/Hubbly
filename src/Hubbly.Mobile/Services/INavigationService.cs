namespace Hubbly.Mobile.Services;

public interface INavigationService
{
    Task NavigateToWelcomeAsync();
    Task NavigateToChatRoomAsync(Guid roomId, string roomTitle);
    Task NavigateToProfileAsync();
    Task NavigateToRoomsAsync();
    Task GoBackAsync();
    Task LogoutAsync();
}