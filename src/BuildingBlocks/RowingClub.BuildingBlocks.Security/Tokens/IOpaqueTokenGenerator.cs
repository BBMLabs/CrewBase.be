namespace RowingClub.BuildingBlocks.Security.Tokens;

/// <summary>Generates cryptographically random, high-entropy opaque tokens (refresh tokens, email
/// verification tokens, password reset tokens - anything that must not be guessable).</summary>
public interface IOpaqueTokenGenerator
{
    string Generate();
}
