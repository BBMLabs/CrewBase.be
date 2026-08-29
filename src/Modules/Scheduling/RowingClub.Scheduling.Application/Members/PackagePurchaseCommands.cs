using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Application.Billing;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Customers;
using RowingClub.Scheduling.Domain.Logs;
using RowingClub.Scheduling.Domain.Packages;

namespace RowingClub.Scheduling.Application.Members;

/// <summary>Üyenin kendi satın alabileceği (aktif VE kampanya penceresi içinde/sınırsız) paket kataloğu.</summary>
public sealed record GetPurchasablePackagesQuery : IRequest<List<PurchasablePackageDto>>;

public sealed record PurchasablePackageDto(
    Guid Id, string Name, string? Description, int SessionCount, decimal Price,
    string? ImagePath, int? ValidityDays);

public sealed class GetPurchasablePackagesQueryHandler(ILessonPackageRepository repository)
    : IRequestHandler<GetPurchasablePackagesQuery, List<PurchasablePackageDto>>
{
    public async Task<List<PurchasablePackageDto>> Handle(
        GetPurchasablePackagesQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var packages = await repository.GetAllAsync(cancellationToken);
        return packages
            .Where(p => p.IsCurrentlyPurchasable(now))
            .OrderBy(p => p.GetEffectivePrice(now))
            .Select(p => new PurchasablePackageDto(
                p.Id, p.Name, p.Description, p.SessionCount, p.GetEffectivePrice(now), p.ImagePath, p.ValidityDays))
            .ToList();
    }
}

/// <summary>
/// Üyenin kendi kartıyla bir ders paketi satın alma akışını başlatır - iyzico checkout formunu
/// döner, kart bilgisi bize hiç ulaşmaz. Paket ancak <see cref="ConfirmPackagePurchaseCommand"/>
/// ile, ödeme onaylandıktan sonra üyenin bakiyesine eklenir (bkz. firma abonelik akışındaki
/// SubscribeToPlan/ConfirmSubscriptionCheckout ile birebir aynı iki-adımlı desen).
/// </summary>
public sealed record PurchasePackageCommand(Guid CustomerId, Guid PackageId, string CallbackUrl, string BuyerIp)
    : ICommand<PurchasePackageResult>;

public sealed record PurchasePackageResult(string CheckoutFormContent, string Token);

/// <summary>iyzico checkout formundan dönüldükten sonra çağrılır; ödeme başarılıysa paket burada üyenin bakiyesine eklenir.</summary>
public sealed record ConfirmPackagePurchaseCommand(Guid CustomerId, Guid PackageId, string Token)
    : ICommand<CustomerPackageDto>;

public sealed class PurchasePackageCommandHandler(
    ICustomerRepository customerRepository,
    ILessonPackageRepository packageRepository,
    IIyzicoPaymentClient iyzicoClient)
    : IRequestHandler<PurchasePackageCommand, PurchasePackageResult>
{
    public async Task<PurchasePackageResult> Handle(PurchasePackageCommand request, CancellationToken cancellationToken)
    {
        if (!iyzicoClient.IsConfigured)
            throw new DomainException("billing_not_configured", "Paket satın alma şu anda yapılandırılmamış.");

        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        var package = await packageRepository.GetByIdAsync(request.PackageId, cancellationToken);
        if (package is null || !package.IsCurrentlyPurchasable(DateTimeOffset.UtcNow))
            throw new DomainException("invalid_package", "Paket bulunamadı veya şu anda satın alınabilir değil.");

        var effectivePrice = package.GetEffectivePrice(DateTimeOffset.UtcNow);
        if (effectivePrice <= 0)
            throw new DomainException("invalid_package_price", "Bu paketin satın alınabilir bir fiyatı yok.");

        var nameParts = customer.FullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var buyer = new IyzicoPaymentBuyer(
            Name: nameParts.Length > 0 ? nameParts[0] : customer.FullName,
            Surname: nameParts.Length > 1 ? nameParts[1] : "-",
            Email: customer.Email ?? "no-reply@faturebase.com",
            IdentityNumber: "11111111111",
            RegistrationAddress: "-",
            City: "Istanbul",
            Country: "Turkey",
            Ip: request.BuyerIp);

        var result = await iyzicoClient.InitializeCheckoutFormAsync(
            customer.Id.ToString(), package.Id.ToString(), package.Name, effectivePrice,
            buyer, request.CallbackUrl, cancellationToken);

        if (string.IsNullOrWhiteSpace(result.CheckoutFormContentHtml))
            throw new DomainException("checkout_init_failed", "Ödeme formu başlatılamadı.");

        return new PurchasePackageResult(result.CheckoutFormContentHtml, result.Token);
    }
}

public sealed class ConfirmPackagePurchaseCommandHandler(
    ICustomerRepository customerRepository,
    ILessonPackageRepository packageRepository,
    ICustomerPackageRepository customerPackageRepository,
    IMemberLogRepository memberLogRepository,
    IIyzicoPaymentClient iyzicoClient,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmPackagePurchaseCommand, CustomerPackageDto>
{
    public async Task<CustomerPackageDto> Handle(ConfirmPackagePurchaseCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken)
            ?? throw new NotFoundException("Customer", request.CustomerId.ToString());

        var package = await packageRepository.GetByIdAsync(request.PackageId, cancellationToken)
            ?? throw new NotFoundException("LessonPackage", request.PackageId.ToString());

        var result = await iyzicoClient.RetrieveCheckoutFormResultAsync(request.Token, cancellationToken);
        if (!result.Success || result.PaymentReferenceCode is null)
            throw new DomainException("checkout_failed", result.ErrorMessage ?? "Ödeme tamamlanamadı.");

        if (await customerPackageRepository.ExistsByPaymentReferenceCodeAsync(result.PaymentReferenceCode, cancellationToken))
        {
            var existing = (await customerPackageRepository.GetByCustomerAsync(request.CustomerId, cancellationToken))
                .First(p => p.PaymentReferenceCode == result.PaymentReferenceCode);
            return GetMemberPackagesQueryHandler.ToDto(existing);
        }

        // iyzico dönüşü gecikmiş olabilir; ödeme başarılı olsa bile bu arada kampanya kapanmış ya
        // da paket pasife alınmışsa yeni bir geçerli paket oluşturulmaz (bkz. plan Aşama 5).
        if (!package.IsCurrentlyPurchasable(DateTimeOffset.UtcNow))
            throw new DomainException(
                "package_no_longer_purchasable",
                "Ödemeniz alındı ancak bu paket artık satın alınamıyor; lütfen firmayla iletişime geçin.");

        var purchased = CustomerPackage.Assign(customer.Id, package, CustomerPackageSource.Purchased, result.PaymentReferenceCode);
        customerPackageRepository.Add(purchased);
        memberLogRepository.Add(MemberLog.Record(
            customer.Id, MemberEvents.PackagePurchased,
            $"{package.Name} ({package.SessionCount} ders, {package.GetEffectivePrice(DateTimeOffset.UtcNow):0.00} TRY)"));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return GetMemberPackagesQueryHandler.ToDto(purchased);
    }
}
