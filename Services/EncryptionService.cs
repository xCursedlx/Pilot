using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PilotApp.Services;

public static class EncryptionService
{
    private const int KeySize = 32;
    private const int IvSize = 16;
    private const int SaltSize = 16;
    private const int Iterations = 100_000;

    public static byte[] Encrypt(string plainText, string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = DeriveKey(password, salt);
        var iv = RandomNumberGenerator.GetBytes(IvSize);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var ms = new MemoryStream();
        ms.Write(salt, 0, SaltSize);
        ms.Write(iv, 0, IvSize);

        using var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);
        var data = Encoding.UTF8.GetBytes(plainText);
        cs.Write(data, 0, data.Length);
        cs.FlushFinalBlock();

        return ms.ToArray();
    }

    public static string Decrypt(byte[] cipherData, string password)
    {
        var salt = cipherData[..SaltSize];
        var iv = cipherData[SaltSize..(SaltSize + IvSize)];
        var cipher = cipherData[(SaltSize + IvSize)..];

        var key = DeriveKey(password, salt);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = iv;

        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
        using var reader = new StreamReader(cs, Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static string EncryptToBase64(string plainText, string password)
        => Convert.ToBase64String(Encrypt(plainText, password));

    public static string DecryptFromBase64(string base64, string password)
        => Decrypt(Convert.FromBase64String(base64), password);

    private static byte[] DeriveKey(string password, byte[] salt)
    {
        using var kdf = new Rfc2898DeriveBytes(
            Encoding.UTF8.GetBytes(password), salt, Iterations, HashAlgorithmName.SHA256);
        return kdf.GetBytes(KeySize);
    }
}