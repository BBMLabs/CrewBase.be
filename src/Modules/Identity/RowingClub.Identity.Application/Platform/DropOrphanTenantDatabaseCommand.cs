using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Platform;

public sealed record DropOrphanTenantDatabaseCommand(string DatabaseName) : ICommand<Unit>;

public sealed class DropOrphanTenantDatabaseCommandHandler(
    ICompanyRepository companyRepository,
    ITenantDatabaseProvisioner tenantDatabaseProvisioner)
    : IRequestHandler<DropOrphanTenantDatabaseCommand, Unit>
{
    public async Task<Unit> Handle(DropOrphanTenantDatabaseCommand request, CancellationToken cancellationToken)
    {
        var companies = await companyRepository.GetAllAsync(cancellationToken);
        if (companies.Any(company => company.DatabaseName == request.DatabaseName))
            throw new DomainException("database_in_use", "Bu veritabanı bir firmaya ait, doğrudan silinemez.");

        try
        {
            await tenantDatabaseProvisioner.DeprovisionAsync(request.DatabaseName, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            throw new DomainException("invalid_database_name", "Geçersiz tenant veritabanı adı.", ex);
        }

        return Unit.Value;
    }
}
