using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.Billing.GetPaymentHistory;

public sealed record GetPaymentHistoryQuery(Guid CompanyId, string? Cursor = null, int Limit = 25)
    : IQuery<KeysetResult<CompanyPaymentDto>>;

public sealed record CompanyPaymentDto(
    string Plan, decimal Amount, string Currency, string Kind, string Status,
    DateTimeOffset OccurredAtUtc, string? FailureReason);
