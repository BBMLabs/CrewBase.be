namespace RowingClub.BuildingBlocks.Domain;

/// <summary>
/// Raised when a domain invariant or business rule is violated. Mapped to HTTP 409 by the
/// global exception handler (docs/SECURITY.md / spec section 18) unless a more specific
/// subclass overrides <see cref="ErrorCode"/> mapping in the API layer.
/// </summary>
public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string errorCode, string message) : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
