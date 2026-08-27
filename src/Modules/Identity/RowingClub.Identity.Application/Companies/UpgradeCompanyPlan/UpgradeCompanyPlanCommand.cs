using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;

namespace RowingClub.Identity.Application.Companies.UpgradeCompanyPlan;

/// <summary>
/// Firma yetkilisinin panelden kendi yaptığı paket değişikliği (yalnızca sabit paketler, hem
/// yükseltme hem düşürme). Kullanım sayıları (UsedBranches/UsedMembers/UsedBoats) çağıran
/// tarafından (API katmanı) tenant veritabanından önceden çözülüp buraya taşınır - Identity
/// modülü tenant DB'ye doğrudan erişemez.
/// </summary>
public sealed record UpgradeCompanyPlanCommand(
    Guid CompanyId, string Plan, int UsedBranches, int UsedMembers, int UsedBoats) : ICommand<UpgradeCompanyPlanResult>;

public sealed record UpgradeCompanyPlanResult(string Plan, int MaxBranches, int MaxMembers, int MaxBoats);
