using FluentValidation;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.Companies.RegisterCompany;

public sealed class RegisterCompanyCommandValidator : AbstractValidator<RegisterCompanyCommand>
{
    public RegisterCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("Şirket adı zorunludur.")
            .MaximumLength(200);

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .MaximumLength(254)
            .Matches(EmailAddress.Pattern).WithMessage("Geçerli bir e-posta adresi giriniz.");

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Parola zorunludur.")
            .MinimumLength(10).WithMessage("Parola en az 10 karakter olmalıdır.")
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Parola en az bir büyük harf içermelidir.")
            .Matches("[a-z]").WithMessage("Parola en az bir küçük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Parola en az bir rakam içermelidir.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Parola en az bir özel karakter içermelidir.");

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$").When(x => x.Phone is not null)
            .WithMessage("Geçerli bir telefon numarası giriniz.");

        RuleFor(x => x.ContactEmail)
            .MaximumLength(254)
            .Matches(EmailAddress.Pattern).When(x => x.ContactEmail is not null)
            .WithMessage("Geçerli bir iletişim e-posta adresi giriniz.");

        RuleFor(x => x.Address)
            .MaximumLength(500);
    }
}
