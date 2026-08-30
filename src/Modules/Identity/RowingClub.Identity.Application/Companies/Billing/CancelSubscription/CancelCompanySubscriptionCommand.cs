using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.Billing.CancelSubscription;

public sealed record CancelCompanySubscriptionCommand(Guid CompanyId) : ICommand<Unit>;
