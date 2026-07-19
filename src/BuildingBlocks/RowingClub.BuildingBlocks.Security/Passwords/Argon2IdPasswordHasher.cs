using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace RowingClub.BuildingBlocks.Security.Passwords;

/// <summary>
/// Argon2id password hashing per spec section 3/9. Encodes salt, hash and the cost parameters
/// used to produce it into one string, so parameters can be strengthened later without breaking
/// verification of passwords hashed under the old ones.
/// </summary>
public sealed class Argon2IdPasswordHasher : IPasswordHasher
{
    private const int SaltSizeBytes = 16;
    private const int HashSizeBytes = 32;
    private const int DegreeOfParallelism = 4;
    private const int Iterations = 3;
    private const int MemorySizeKb = 65536; // 64 MB

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        var hash = ComputeHash(password, salt, Iterations, MemorySizeKb, DegreeOfParallelism);

        return string.Join(
            '$',
            "argon2id",
            $"v=19",
            $"m={MemorySizeKb},t={Iterations},p={DegreeOfParallelism}",
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash));
    }

    public bool Verify(string password, string encodedHash)
    {
        var parts = encodedHash.Split('$');
        if (parts.Length != 5 || parts[0] != "argon2id")
        {
            return false;
        }

        var costParameters = parts[2].Split(',');
        var memoryKb = int.Parse(costParameters[0].Split('=')[1]);
        var iterations = int.Parse(costParameters[1].Split('=')[1]);
        var parallelism = int.Parse(costParameters[2].Split('=')[1]);

        var salt = Convert.FromBase64String(parts[3]);
        var expectedHash = Convert.FromBase64String(parts[4]);

        var actualHash = ComputeHash(password, salt, iterations, memoryKb, parallelism, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static byte[] ComputeHash(
        string password,
        byte[] salt,
        int iterations,
        int memoryKb,
        int parallelism,
        int hashSizeBytes = HashSizeBytes)
    {
        using var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = parallelism,
            Iterations = iterations,
            MemorySize = memoryKb,
        };

        return argon2.GetBytes(hashSizeBytes);
    }
}
