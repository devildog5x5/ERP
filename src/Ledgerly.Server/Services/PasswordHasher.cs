using System;
using System.Security.Cryptography;

namespace Ledgerly.Server.Services;

public static class PasswordHasher
{
    public static (string Hash, string Salt) Hash(string password)
    {
        var saltBytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(saltBytes);
        var salt = Convert.ToBase64String(saltBytes);
        var hash = Compute(password, salt);
        return (hash, salt);
    }

    public static bool Verify(string password, string hash, string salt) =>
        string.Equals(Compute(password, salt), hash, StringComparison.Ordinal);

    private static string Compute(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 10000);
        return Convert.ToBase64String(pbkdf2.GetBytes(32));
    }
}
