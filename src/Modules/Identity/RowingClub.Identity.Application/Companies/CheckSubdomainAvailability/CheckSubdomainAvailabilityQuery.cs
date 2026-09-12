using MediatR;

namespace RowingClub.Identity.Application.Companies.CheckSubdomainAvailability;

public sealed record CheckSubdomainAvailabilityQuery(string Subdomain) : IRequest<bool>;
