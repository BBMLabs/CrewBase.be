using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.Billing.CancelPendingDowngrade;

public sealed record CancelPendingDowngradeCommand(Guid CompanyId) : ICommand<Unit>;
