using FluentValidation;

namespace RowingClub.Identity.Application.Login;

public sealed class VerifyTwoFactorLoginCommandValidator : AbstractValidator<VerifyTwoFactorLoginCommand>
{
    public VerifyTwoFactorLoginCommandValidator()
    {
        RuleFor(x => x.PendingToken).NotEmpty();
        RuleFor(x => x.Code).NotEmpty();
    }
}
