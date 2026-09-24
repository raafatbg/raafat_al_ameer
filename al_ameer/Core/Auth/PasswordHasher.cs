using System.Security.Cryptography;

namespace al_ameer.Auth;

public static class PasswordHasher
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string storedHash, out bool requiresUpgrade)
    {
        requiresUpgrade = false;
        string[] parts = storedHash.Split('$');
        if (parts.Length == 4 && parts[0] == "PBKDF2-SHA256" && int.TryParse(parts[1], out int iterations))
        {
            try
            {
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expected = Convert.FromBase64String(parts[3]);
                byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            catch (FormatException) { return false; }
        }

        // One-time compatibility path for existing plaintext rows. A successful login replaces it immediately.
        requiresUpgrade = CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(password), System.Text.Encoding.UTF8.GetBytes(storedHash));
        return requiresUpgrade;
    }
}
