namespace RowingClub.BuildingBlocks.Security.Passwords;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string encodedHash);
}
