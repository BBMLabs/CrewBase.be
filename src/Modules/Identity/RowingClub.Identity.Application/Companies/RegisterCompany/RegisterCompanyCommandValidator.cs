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

        RuleFor(x => x.TaxNumber)
            .NotEmpty().WithMessage("Vergi numarası zorunludur.")
            .Matches(@"^\d{10}$|^\d{11}$")
            .WithMessage("Geçerli bir vergi numarası veya T.C. kimlik numarası giriniz (10 veya 11 haneli).");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefon zorunludur.")
            .MaximumLength(20)
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$").WithMessage("Geçerli bir telefon numarası giriniz.");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("İletişim e-postası zorunludur.")
            .MaximumLength(254)
            .Matches(EmailAddress.Pattern).WithMessage("Geçerli bir iletişim e-posta adresi giriniz.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Adres zorunludur.")
            .MaximumLength(500);
    }
}
