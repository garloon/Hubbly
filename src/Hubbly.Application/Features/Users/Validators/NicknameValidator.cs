using FluentValidation;

namespace Hubbly.Application.Features.Users.Validators;

public class NicknameValidator : AbstractValidator<string>
{
    public NicknameValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Никнейм не может быть пустым")
            .Length(3, 50).WithMessage("Никнейм должен быть от 3 до 50 символов")
            .Matches(@"^[a-zA-Zа-яА-Я0-9_\s]+$").WithMessage("Можно использовать только буквы, цифры, пробелы и _")
            .Must(NotBeReserved).WithMessage("Этот никнейм зарезервирован");
    }

    private bool NotBeReserved(string nickname)
    {
        var reserved = new[] { "admin", "moderator", "system", "support", "null", "undefined" };
        return !reserved.Contains(nickname.ToLower());
    }
}