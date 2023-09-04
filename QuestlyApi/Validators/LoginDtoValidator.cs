using FluentValidation;
using QuestlyApi.Dtos;

namespace QuestlyApi.Validators;

public abstract class LoginDtoValidator : AbstractValidator<LoginDto>
{
    protected LoginDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}