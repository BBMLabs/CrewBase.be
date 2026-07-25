using MediatR;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Users;
using RowingClub.Identity.Domain.ValueObjects;

namespace RowingClub.Identity.Application.Companies.RegisterCompany;

public sealed class RegisterCompanyCommandHandler(
    ICompanyRepository companyRepository,
    IUserRepository userRepository,
    ICredentialRepository credentialRepository,
    IPasswordHasher passwordHasher)
    : IRequestHandler<RegisterCompanyCommand, RegisterCompanyResponse>
{
    public async Task<RegisterCompanyResponse> Handle(
        RegisterCompanyCommand request, CancellationToken cancellationToken)
    {
        if (await companyRepository.ExistsByNameAsync(request.CompanyName, cancellationToken))
            throw new DomainException("company_name_taken", "Bu şirket adı zaten kullanılıyor.");

        var email = EmailAddress.Create(request.AdminEmail);

        if (await userRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new DomainException("email_already_registered", "Bu e-posta adresi zaten kayıtlı.");

        var company = Company.Register(request.CompanyName, request.Phone, request.ContactEmail, request.Address);
        companyRepository.Add(company);

        var adminUser = User.RegisterCompanyAdmin(email, company.Id);
        var credential = Credential.Create(adminUser.Id, passwordHasher.Hash(request.AdminPassword));

        userRepository.Add(adminUser);
        credentialRepository.Add(credential);

        return new RegisterCompanyResponse(
            company.Id, adminUser.Id, company.Name, adminUser.Email.Value, company.CreatedAtUtc);
    }
}
