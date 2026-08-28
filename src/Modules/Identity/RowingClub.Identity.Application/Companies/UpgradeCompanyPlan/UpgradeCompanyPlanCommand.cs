using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.UpgradeCompanyPlan;

/// <summary>
/// Firma yetkilisinin panelden kendi yaptığı ÜST pakete geçiş (alt pakete geçiş artık
/// RequestPlanDowngradeCommand ile - dönem sonuna ertelenir). Aktif bir iyzico aboneliği
/// üzerinden aradaki fark anında tahsil edilir (proration iyzico tarafında hesaplanır).
/// Kullanım sayıları (UsedBranches/UsedMembers/UsedBoats) çağıran tarafından (API katmanı)
/// tenant veritabanından önceden çözülüp buraya taşınır - Identity modülü tenant DB'ye doğrudan
/// erişemez. IdempotencyKey ile aynı tıklamanın iki kez iyzico'ya tekrar tahsilat yaptırması
/// engellenir (bkz. IdempotencyBehavior).
/// </summary>
public sealed record UpgradeCompanyPlanCommand(
    Guid CompanyId, string Plan, int UsedBranches, int UsedMembers, int UsedBoats, string IdempotencyKey)
    : ICommand<UpgradeCompanyPlanResult>, IIdempotentCommand;

public sealed record UpgradeCompanyPlanResult(
    string Plan, int MaxBranches, int MaxMembers, int MaxBoats,
    decimal ChargedAmount, DateTimeOffset? NextPaymentDateUtc);
