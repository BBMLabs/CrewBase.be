namespace RowingClub.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Mevcut HTTP isteğinin ağ bağlamı - istek gövdesi/kimlik claim'leri değil, isteğin NEREDEN
/// geldiği (IP, User-Agent). ActivityLog kaydına ve IP engelleme kontrolüne (BlockedIpBehavior)
/// aktarılır. HTTP dışı bir bağlamda (ör. arka plan işleri) her iki alan da null döner.
/// </summary>
public interface ICurrentRequestContext
{
    string? IpAddress { get; }

    string? UserAgent { get; }
}
