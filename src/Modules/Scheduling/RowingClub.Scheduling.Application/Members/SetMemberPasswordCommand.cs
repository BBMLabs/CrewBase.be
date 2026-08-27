using FluentValidation;
using MediatR;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.BuildingBlocks.Security.Passwords;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Customers;

namespace RowingClub.Scheduling.Application.Members;

/// <summary>
/// Firma panelinden e-postalı eklenen bir üyenin, kendisine gönderilen bağlantıdaki tek kullanımlık
/// token ile ilk şifresini belirlemesi (veya - aynı mekanizma - daha sonra yeniden şifre
/// oluşturması). Panelden üretilmiş bir şifre asla yoktur; token her zaman üyenin kendi eylemiyle
/// (bağlantıya tıklaması) tüketilir.
/// </summary>
public sealed record SetMemberPasswordCommand(string Email, string Token, string NewPassword) : ICommand<Unit>;

public sealed class SetMemberPasswordCommandValidator : AbstractValidator<SetMemberPasswordCommand>
{
    public SetMemberPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(10).WithMessage("Şifre en az 10 karakter olmalıdır.")
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Şifre en az bir büyük harf içermelidir.")
            .Matches("[a-z]").WithMessage("Şifre en az bir küçük harf içermelidir.")
            .Matches("[0-9]").WithMessage("Şifre en az bir rakam içermelidir.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Şifre en az bir özel karakter içermelidir.");
    }
}

public sealed class SetMemberPasswordCommandHandler(
    ICustomerRepository customerRepository,
    IMemberPasswordSetupTokenRepository setupTokenRepository,
    IRefreshTokenHasher tokenHasher,
    IPasswordHasher passwordHasher,
    ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<SetMemberPasswordCommand, Unit>
{
    public async Task<Unit> Handle(SetMemberPasswordCommand request, CancellationToken cancellationToken)
    {
        var invalid = new DomainException("invalid_password_setup", "Şifre oluşturma bağlantısı geçersiz veya süresi dolmuş.");

        var customer = await customerRepository.GetByEmailAsync(request.Email, cancellationToken) ?? throw invalid;

        var tokenHash = tokenHasher.Hash(request.Token);
        var setupToken = await setupTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken) ?? throw invalid;

        if (setupToken.CustomerId != customer.Id || !setupToken.IsValid)
            throw invalid;

        setupToken.MarkUsed();
        customer.AttachAccount(request.Email, passwordHasher.Hash(request.NewPassword));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
