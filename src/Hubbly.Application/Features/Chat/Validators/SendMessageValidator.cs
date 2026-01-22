using FluentValidation;

namespace Hubbly.Application.Features.Chat.Validators;

public class SendMessageValidator : AbstractValidator<string>
{
    public SendMessageValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Сообщение не может быть пустым")
            .MaximumLength(2000).WithMessage("Сообщение не может превышать 2000 символов")
            .MinimumLength(1).WithMessage("Введите хотя бы один символ")
            .Must(BeValidMessage).WithMessage("Сообщение содержит недопустимые символы");
    }

    private bool BeValidMessage(string text)
    {
        // Базовая проверка на управляющие символы (кроме переноса строки)
        return !text.Any(c => char.IsControl(c) && c != '\n' && c != '\r');
    }
}