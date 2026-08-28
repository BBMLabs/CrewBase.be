using MediatR;
using Microsoft.Extensions.Logging;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Files;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Boats;
using RowingClub.Scheduling.Domain.Branches;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Instructors;
using RowingClub.Scheduling.Application.Members;

namespace RowingClub.Scheduling.Application.Panel;

/// <summary>
/// Bir şubeyi kalıcı olarak siler (logo görseli dahil). Üyeler ya başka bir şubeye aktarılır ya da
/// silinir (ikisinden biri zorunlu); şubenin tekneleri/eğitmenleri de her durumda kalıcı olarak
/// silinir (hard delete - bkz. DeleteBoatCommand/DeleteInstructorCommand ile aynı davranış).
/// Ders paketleri firma geneli olduğundan (Branch ile ilişkisi yoktur) bu işlemden ETKİLENMEZ.
/// </summary>
public sealed record DeleteBranchCommand(
    Guid BranchId, string MemberAction, Guid? TransferTargetBranchId,
    string CompanyName, string Subdomain) : ICommand<DeleteBranchResultDto>;

public sealed record DeleteBranchResultDto(
    int TransferredMemberCount, int DeletedMemberCount,
    int DeletedBoatCount, int DeletedInstructorCount);

public sealed class DeleteBranchCommandHandler(
    IBranchRepository branchRepository,
    ICustomerRepository customerRepository,
    IBoatRepository boatRepository,
    IInstructorRepository instructorRepository,
    IMemberBranchTransferEmailSender transferEmailSender,
    IFileStorageService fileStorage,
    ISchedulingUnitOfWork unitOfWork,
    ITenantDatabase tenantDatabase,
    ILogger<DeleteBranchCommandHandler> logger)
    : IRequestHandler<DeleteBranchCommand, DeleteBranchResultDto>
{
    public async Task<DeleteBranchResultDto> Handle(DeleteBranchCommand request, CancellationToken cancellationToken)
    {
        var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken)
            ?? throw new NotFoundException("Branch", request.BranchId.ToString());

        var members = (await customerRepository.GetAllAsync(cancellationToken))
            .Where(c => c.BranchId == branch.Id)
            .ToList();

        var transferredCount = 0;
        var deletedCount = 0;
        Branch? targetBranch = null;
        List<Customer> emailable = [];

        switch (request.MemberAction)
        {
            case "transfer":
                if (members.Count > 0)
                {
                    if (request.TransferTargetBranchId is not { } targetId || targetId == branch.Id)
                        throw new DomainException("invalid_target_branch", "Aktarılacak bir hedef şube seçmelisiniz.");

                    targetBranch = await branchRepository.GetByIdAsync(targetId, cancellationToken);
                    if (targetBranch is null || !targetBranch.IsActive)
                        throw new DomainException("target_branch_not_found", "Hedef şube bulunamadı veya aktif değil.");

                    // Aktarım toplam üye sayısını DEĞİŞTİRMEZ (aynı firma içinde şube değişimi); yine de
                    // firma zaten planının üzerindeyse (ör. zorla düşürme sonrası) bu işlem engellenir.
                    var usedMembers = await customerRepository.CountAsync(cancellationToken);
                    if (usedMembers > tenantDatabase.MaxMembers)
                        throw new DomainException("member_capacity_insufficient",
                            $"Paketiniz yetersiz; {members.Count} üye aktarılamıyor (kullanım {usedMembers}/{tenantDatabase.MaxMembers}). " +
                            "Üye listesini Excel olarak dışa aktarabilir veya üyeleri silmeyi seçebilirsiniz.");

                    foreach (var member in members)
                    {
                        member.SetBranch(targetBranch.Id);
                        if (!string.IsNullOrWhiteSpace(member.Email))
                            emailable.Add(member);
                    }
                    transferredCount = members.Count;
                }
                break;

            case "delete":
                foreach (var member in members)
                    customerRepository.Remove(member);
                deletedCount = members.Count;
                break;

            default:
                throw new DomainException("invalid_member_action", "Üye aksiyonu 'transfer' veya 'delete' olmalıdır.");
        }

        var boats = (await boatRepository.GetAllAsync(cancellationToken))
            .Where(b => b.BranchId == branch.Id).ToList();
        foreach (var boat in boats)
            boatRepository.Remove(boat);

        var instructors = (await instructorRepository.GetAllAsync(cancellationToken))
            .Where(i => i.BranchId == branch.Id).ToList();
        foreach (var instructor in instructors)
            instructorRepository.Remove(instructor);

        if (branch.LogoPath is { } logoPath)
            await fileStorage.DeleteAsync(logoPath, cancellationToken);

        branchRepository.Remove(branch);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Aktarım e-postaları kaydın kendisinden SONRA, en iyi çaba (best-effort) gönderilir - SMTP
        // geçici olarak erişilemez olsa da aktarım işlemi geri alınmaz (bkz. RequestMemberPasswordResetCommand).
        if (targetBranch is not null)
        {
            foreach (var member in emailable)
            {
                try
                {
                    await transferEmailSender.SendAsync(
                        member.Email!, member.FullName, request.CompanyName, branch.Name, targetBranch.Name,
                        request.Subdomain, targetBranch.Code, cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Şube aktarım e-postası gönderilemedi: {CustomerId}", member.Id);
                }
            }
        }

        return new DeleteBranchResultDto(transferredCount, deletedCount, boats.Count, instructors.Count);
    }
}
