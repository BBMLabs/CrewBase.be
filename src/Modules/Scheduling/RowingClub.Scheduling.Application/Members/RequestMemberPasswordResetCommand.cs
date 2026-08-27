using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Security.Tokens;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Customers;

namespace RowingClub.Scheduling.Application.Members;

/// <summary>
/// Üye giriş ekranından "şifremi unuttum" talebi. E-postanın kayıtlı olup olmadığı sızdırılmaz
/// (user enumeration önlemi) - üye bulunamasa da her zaman başarılı döner.
/// </summary>
public sealed record RequestMemberPasswordResetCommand(
    string Email, string CompanyName, string Subdomain) : ICommand<Unit>;

public sealed class RequestMemberPasswordResetCommandValidator : AbstractValidator<RequestMemberPasswordResetCommand>
{
    public RequestMemberPasswordResetCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}

public sealed class RequestMemberPasswordResetCommandHandler(
    ICustomerRepository customerRepository,
    IMemberPasswordSetupTokenRepository setupTokenRepository,
    IOpaqueTokenGenerator tokenGenerator,
    IRefreshTokenHasher tokenHasher,
    IMemberWelcomeEmailSender welcomeEmailSender,
    ISchedulingUnitOfWork unitOfWork,
    ILogger<RequestMemberPasswordResetCommandHandler> logger)
    : IRequestHandler<RequestMemberPasswordResetCommand, Unit>
{
    public async Task<Unit> Handle(RequestMemberPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (customer is null || string.IsNullOrWhiteSpace(customer.Email))
            return Unit.Value;

        var rawToken = tokenGenerator.Generate();
        setupTokenRepository.Add(
            MemberPasswordSetupToken.Issue(customer.Id, tokenHasher.Hash(rawToken), TimeSpan.FromHours(1)));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await welcomeEmailSender.SendPasswordResetAsync(
                customer.Email, customer.FullName, request.CompanyName, rawToken, request.Subdomain, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Üye şifre sıfırlama e-postası gönderilemedi: {CustomerId}", customer.Id);
        }

        return Unit.Value;
    }
}
