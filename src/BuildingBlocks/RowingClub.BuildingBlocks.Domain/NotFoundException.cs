namespace RowingClub.BuildingBlocks.Domain;

/// <summary>Mapped to HTTP 404 by the global exception handler (spec section 18).</summary>
public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public static NotFoundException For<TEntity>(object key) =>
        new(
            $"{typeof(TEntity).Name.ToLowerInvariant()}_not_found",
            $"{typeof(TEntity).Name} '{key}' bulunamadı.");
}
