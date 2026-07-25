using FluentValidation;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.EmailVerification;

public sealed class SendVerificationEmailCommandValidator : AbstractValidator<SendVerificationEmailCommand>
{
    public SendVerificationEmailCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .Matches(EmailAddress.Pattern).WithMessage("'Email' geçerli bir e-posta adresi değil.");
    }
}
