using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.Billing.RecordManualPaymentCorrection;

public sealed record RecordManualPaymentCorrectionCommand(
    Guid CompanyId, decimal Amount, string Currency, string Kind, string Status, string Note)
    : ICommand<Unit>;
