using FluentValidation;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.Companies.UpdateCompany;

public sealed class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Şirket adı zorunludur.")
            .MaximumLength(200);

        RuleFor(x => x.ContactEmail)
            .MaximumLength(254)
            .Matches(EmailAddress.Pattern).WithMessage("Geçerli bir iletişim e-posta adresi giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

        RuleFor(x => x.Phone)
            .MaximumLength(20)
            .Matches(@"^\+?[0-9\s\-\(\)]{7,20}$").WithMessage("Geçerli bir telefon numarası giriniz.")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Address).MaximumLength(500);

        RuleFor(x => x.TaxNumber)
            .Matches(@"^\d{10}$|^\d{11}$")
            .WithMessage("Geçerli bir vergi numarası veya T.C. kimlik numarası giriniz (10 veya 11 haneli).")
            .When(x => !string.IsNullOrWhiteSpace(x.TaxNumber));
    }
}
