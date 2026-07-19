namespace RowingClub.BuildingBlocks.Domain;

/// <summary>Mapped to HTTP 401 by the global exception handler (spec section 18). Always uses a
/// generic message - never reveal whether the email or the password was the wrong part
/// (spec section 11 brute-force guidance).</summary>
public class AuthenticationFailedException : Exception
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }
}
