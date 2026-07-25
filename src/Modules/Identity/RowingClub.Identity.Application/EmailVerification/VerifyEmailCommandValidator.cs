using FluentValidation;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.EmailVerification;

public sealed class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .Matches(EmailAddress.Pattern).WithMessage("'Email' geçerli bir e-posta adresi değil.");
        RuleFor(x => x.Token).NotEmpty();
    }
}
