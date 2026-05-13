using System;
using System.Security.Cryptography;
using System.Text;

namespace PilotApp.Services;

public static class PasswordService
{
    public static string Hash(string password)
    {
        var salt = Guid.NewGuid().ToString("N");
        var hash = ComputeHash(salt, password);
        return $"{salt}:{hash}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split(':');
        if (parts.Length != 2) return false;
        var hash = ComputeHash(parts[0], password);
        return hash == parts[1];
    }

    private static string ComputeHash(string salt, string password)
    {
        var bytes = Encoding.UTF8.GetBytes(salt + password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLower();
    }
}