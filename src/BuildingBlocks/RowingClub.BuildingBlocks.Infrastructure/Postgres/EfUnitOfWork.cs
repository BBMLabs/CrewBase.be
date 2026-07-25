using RowingClub.BuildingBlocks.Application.Abstractions;

namespace RowingClub.BuildingBlocks.Infrastructure.Postgres;

public sealed class EfUnitOfWork(RowingClubDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken) =>
        await context.SaveChangesAsync(cancellationToken);
}
