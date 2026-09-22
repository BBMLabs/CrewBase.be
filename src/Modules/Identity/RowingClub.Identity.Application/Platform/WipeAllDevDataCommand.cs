using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Companies;
using RowingClub.Identity.Domain.Companies.Billing;
using RowingClub.Identity.Domain.Tokens;
using RowingClub.Identity.Domain.Users;

namespace RowingClub.Identity.Application.Platform;

public sealed record WipeAllDevDataCommand : ICommand<WipeAllDevDataResult>;

public sealed record WipeAllDevDataResult(
    int CompaniesDeleted,
    int TenantDatabasesDropped,
    int TenantDatabaseDropFailures,
    int OrphanDatabasesDropped,
    int OrphanDatabaseDropFailures);

public sealed class WipeAllDevDataCommandHandler(
    IHostEnvironment environment,
    ICompanyRepository companyRepository,
    IUserRepository userRepository,
    ICredentialRepository credentialRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ICompanySubscriptionRepository companySubscriptionRepository,
    ICompanyPaymentRepository companyPaymentRepository,
    ITenantDatabaseProvisioner tenantDatabaseProvisioner,
    ILogger<WipeAllDevDataCommandHandler> logger)
    : IRequestHandler<WipeAllDevDataCommand, WipeAllDevDataResult>
{
    public async Task<WipeAllDevDataResult> Handle(WipeAllDevDataCommand request, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
            throw new DomainException("dev_only_operation", "Bu işlem yalnızca geliştirme ortamında çalıştırılabilir.");

        var companies = await companyRepository.GetAllAsync(cancellationToken);

        var companiesDeleted = 0;
        var tenantDatabasesDropped = 0;
        var tenantDatabaseDropFailures = 0;

        foreach (var company in companies)
        {
            var users = await userRepository.GetByCompanyIdAsync(company.Id, cancellationToken);
            foreach (var user in users)
            {
                var refreshTokens = await refreshTokenRepository.GetByUserIdAsync(user.Id, cancellationToken);
                refreshTokenRepository.RemoveRange(refreshTokens);

                var credential = await credentialRepository.GetByUserIdAsync(user.Id, cancellationToken);
                if (credential is not null)
                    credentialRepository.Remove(credential);

                userRepository.Remove(user);
            }

            var subscription = await companySubscriptionRepository.GetByCompanyIdAsync(company.Id, cancellationToken);
            if (subscription is not null)
                companySubscriptionRepository.Remove(subscription);

            var payments = await companyPaymentRepository.GetAllByCompanyIdAsync(company.Id, cancellationToken);
            companyPaymentRepository.RemoveRange(payments);

            companyRepository.Remove(company);
            companiesDeleted++;

            try
            {
                await tenantDatabaseProvisioner.DeprovisionAsync(company.DatabaseName, cancellationToken);
                tenantDatabasesDropped++;
            }
            catch (Exception ex)
            {
                tenantDatabaseDropFailures++;
                logger.LogError(ex, "{Company} için tenant veritabanı silinemedi.", company.Name);
            }
        }

        var remainingDatabaseNames = await tenantDatabaseProvisioner.ListTenantDatabaseNamesAsync(cancellationToken);

        var orphanDatabasesDropped = 0;
        var orphanDatabaseDropFailures = 0;
        foreach (var databaseName in remainingDatabaseNames)
        {
            try
            {
                await tenantDatabaseProvisioner.DeprovisionAsync(databaseName, cancellationToken);
                orphanDatabasesDropped++;
            }
            catch (Exception ex)
            {
                orphanDatabaseDropFailures++;
                logger.LogError(ex, "Yetim tenant veritabanı silinemedi: {DatabaseName}", databaseName);
            }
        }

        return new WipeAllDevDataResult(
            companiesDeleted,
            tenantDatabasesDropped,
            tenantDatabaseDropFailures,
            orphanDatabasesDropped,
            orphanDatabaseDropFailures);
    }
}
