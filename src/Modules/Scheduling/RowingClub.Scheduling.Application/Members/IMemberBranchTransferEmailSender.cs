namespace RowingClub.Scheduling.Application.Members;

/// <summary>
/// Bir üye, şube silme akışında başka bir şubeye aktarıldığında yeni şubesini/site adresini
/// bildiren e-posta. Kompozisyon katmanında (API) Identity'nin SMTP altyapısına bağlanır -
/// bkz. <see cref="IMemberWelcomeEmailSender"/> için aynı desen.
/// </summary>
public interface IMemberBranchTransferEmailSender
{
    Task SendAsync(
        string email, string fullName, string companyName, string oldBranchName, string newBranchName,
        string subdomain, string newBranchCode, CancellationToken cancellationToken);
}
