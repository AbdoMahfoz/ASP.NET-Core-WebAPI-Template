using System;
using BusinessLogic.Interfaces;
using System.Security.Cryptography;

namespace BusinessLogic.Implementations;

public class Rfc2898PasswordManager : IPasswordManager
{
    public bool ComparePassword(string vanilla, string hashed)
    {
        var hashBytes = Convert.FromBase64String(hashed);
        var salt = new byte[16];
        Array.Copy(hashBytes, 0, salt, 0, 16);
        var pbkdf2 = new Rfc2898DeriveBytes(vanilla, salt, 10000, HashAlgorithmName.SHA1);
        var hash = pbkdf2.GetBytes(20);
        for (var i = 0; i < 20; i++)
        {
            if (hashBytes[i + 16] != hash[i])
            {
                return false;
            }
        }
        return true;
    }
    public string HashPassword(string password)
    {
        byte[] salt;
        RandomNumberGenerator.Create().GetBytes(salt = new byte[16]);
        var derivedBytes = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA1);
        var hash = derivedBytes.GetBytes(20);
        var hashBytes = new byte[36];
        Array.Copy(salt, 0, hashBytes, 0, 16);
        Array.Copy(hash, 0, hashBytes, 16, 20);
        return Convert.ToBase64String(hashBytes);
    }
}