using System.Security.Cryptography;
using Bartrix.BuildingBlocks.Authentication;

namespace Bartrix.Modules.Auth.Infrastructure;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int IterationCount = 100_000;
    private const string FormatMarker = "pbkdf2-v1";

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            IterationCount,
            HashAlgorithmName.SHA256,
            KeySize);

        return string.Join('.', FormatMarker, IterationCount, Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    public bool Verify(string hashedPassword, string providedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(providedPassword);

        var parts = hashedPassword.Split('.', 4);
        if (parts.Length != 4 || !string.Equals(parts[0], FormatMarker, StringComparison.Ordinal))
        {
            return false;
        }

        if (!int.TryParse(parts[1], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[2]);
        var expectedKey = Convert.FromBase64String(parts[3]);

        var actualKey = Rfc2898DeriveBytes.Pbkdf2(
            providedPassword,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            expectedKey.Length);

        return CryptographicOperations.FixedTimeEquals(actualKey, expectedKey);
    }
}
