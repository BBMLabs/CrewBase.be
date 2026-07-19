using FluentValidation;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.Login;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .Matches(EmailAddress.Pattern).WithMessage("'Email' geçerli bir e-posta adresi değil.");
        RuleFor(x => x.Password).NotEmpty();
    }
}
