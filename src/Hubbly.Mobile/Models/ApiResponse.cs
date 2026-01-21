namespace Hubbly.Mobile.Models;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }

    // Добавим метод для дебага
    public override string ToString()
    {
        return $"Success: {Success}, Error: {Error}, Data: {Data}";
    }
}