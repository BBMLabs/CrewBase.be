using System.Net;
using MediatR;
using RowingClub.BuildingBlocks.Application.Abstractions;
using RowingClub.BuildingBlocks.Application.Messaging;
using RowingClub.BuildingBlocks.Domain;
using RowingClub.Scheduling.Domain;
using RowingClub.Scheduling.Domain.Logs;

namespace RowingClub.Scheduling.Application.Panel;

public sealed record BlockedIpAddressDto(Guid Id, string IpAddress, string? Reason, DateTimeOffset BlockedAtUtc);

public sealed record GetBlockedIpAddressesQuery : IRequest<List<BlockedIpAddressDto>>;

public sealed record BlockIpAddressCommand(string IpAddress, string? Reason)
    : ICommand<BlockedIpAddressDto>, IBypassIpBlockCheck;

public sealed record UnblockIpAddressCommand(string IpAddress) : ICommand<Unit>, IBypassIpBlockCheck;

public sealed class GetBlockedIpAddressesQueryHandler(IBlockedIpAddressRepository repository)
    : IRequestHandler<GetBlockedIpAddressesQuery, List<BlockedIpAddressDto>>
{
    public async Task<List<BlockedIpAddressDto>> Handle(GetBlockedIpAddressesQuery request, CancellationToken cancellationToken)
    {
        var entries = await repository.GetAllAsync(cancellationToken);
        return entries.Select(ToDto).ToList();
    }

    internal static BlockedIpAddressDto ToDto(BlockedIpAddress entry) =>
        new(entry.Id, entry.IpAddress, entry.Reason, entry.BlockedAtUtc);
}

public sealed class BlockIpAddressCommandHandler(
    IBlockedIpAddressRepository repository, ISchedulingUnitOfWork unitOfWork, ICurrentUser currentUser)
    : IRequestHandler<BlockIpAddressCommand, BlockedIpAddressDto>
{
    public async Task<BlockedIpAddressDto> Handle(BlockIpAddressCommand request, CancellationToken cancellationToken)
    {
        if (!IPAddress.TryParse(request.IpAddress, out _))
            throw new DomainException("invalid_ip_address", "Geçerli bir IP adresi giriniz.");

        var existing = await repository.GetByIpAsync(request.IpAddress, cancellationToken);
        if (existing is not null)
            return GetBlockedIpAddressesQueryHandler.ToDto(existing);

        var entry = BlockedIpAddress.Create(request.IpAddress, request.Reason, currentUser.UserId);
        repository.Add(entry);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return GetBlockedIpAddressesQueryHandler.ToDto(entry);
    }
}

public sealed class UnblockIpAddressCommandHandler(IBlockedIpAddressRepository repository, ISchedulingUnitOfWork unitOfWork)
    : IRequestHandler<UnblockIpAddressCommand, Unit>
{
    public async Task<Unit> Handle(UnblockIpAddressCommand request, CancellationToken cancellationToken)
    {
        var existing = await repository.GetByIpAsync(request.IpAddress, cancellationToken);
        if (existing is not null)
        {
            repository.Remove(existing);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Unit.Value;
    }
}
