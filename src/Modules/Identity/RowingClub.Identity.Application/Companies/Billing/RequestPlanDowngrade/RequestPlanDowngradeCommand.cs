using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.Billing.RequestPlanDowngrade;

/// <summary>
/// Alt pakete geçiş talebi: anında uygulanmaz, mevcut faturalama döneminin sonunda uygulanır
/// (bkz. CompanySubscription.PendingPlan). UsedBranches/Members/Boats çağıran tarafından
/// (API katmanı) tenant veritabanından önceden çözülüp taşınır.
/// </summary>
public sealed record RequestPlanDowngradeCommand(
    Guid CompanyId, string Plan, int UsedBranches, int UsedMembers, int UsedBoats) : ICommand<RequestPlanDowngradeResult>;

public sealed record RequestPlanDowngradeResult(string PendingPlan, DateTimeOffset EffectiveAtUtc);
