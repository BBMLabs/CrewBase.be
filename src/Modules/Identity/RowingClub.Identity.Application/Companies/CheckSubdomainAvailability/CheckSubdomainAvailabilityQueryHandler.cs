using MediatR;
using RowingClub.Identity.Domain.Companies;

namespace RowingClub.Identity.Application.Companies.CheckSubdomainAvailability;

public sealed class CheckSubdomainAvailabilityQueryHandler(ICompanyRepository companyRepository)
    : IRequestHandler<CheckSubdomainAvailabilityQuery, bool>
{
    public async Task<bool> Handle(CheckSubdomainAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var normalized = request.Subdomain.Trim().ToLowerInvariant();

        if (!SubdomainSlug.IsValidUserSubdomain(normalized) || SubdomainSlug.IsReserved(normalized))
            return false;

        return !await companyRepository.ExistsBySubdomainAsync(normalized, cancellationToken);
    }
}
