using FluentValidation;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.PasswordReset;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(254)
            .Matches(EmailAddress.Pattern).WithMessage("Geçerli bir e-posta adresi giriniz.");
    }
}
