using FluentValidation;
using QuestlyApi.Dtos;

namespace QuestlyApi.Validators;

public abstract class SignUpDtoValidator : AbstractValidator<SignUpDto>
{
    protected SignUpDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
        RuleFor(x => x.ConfirmPassword).Equal(x => x.Password).WithMessage("Passwords must match");
    }
}