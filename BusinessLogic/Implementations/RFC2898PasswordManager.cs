using System;
using BusinessLogic.Interfaces;
using System.Security.Cryptography;

namespace BusinessLogic.Implementations
{
    public class RFC2898PasswordManager : IPasswordManager
    {
        public bool ComparePassword(string Vanilla, string Hashed)
        {
            byte[] hashBytes = Convert.FromBase64String(Hashed);
            byte[] salt = new byte[16];
            Array.Copy(hashBytes, 0, salt, 0, 16);
            var pbkdf2 = new Rfc2898DeriveBytes(Vanilla, salt, 10000);
            byte[] hash = pbkdf2.GetBytes(20);
            for (int i = 0; i < 20; i++)
            {
                if (hashBytes[i + 16] != hash[i])
                {
                    return false;
                }
            }
            return true;
        }
        public string HashPassword(string Password)
        {
            byte[] salt;
            new RNGCryptoServiceProvider().GetBytes(salt = new byte[16]);
            var DerivedBytes = new Rfc2898DeriveBytes(Password, salt, 10000);
            byte[] hash = DerivedBytes.GetBytes(20);
            byte[] hashBytes = new byte[36];
            Array.Copy(salt, 0, hashBytes, 0, 16);
            Array.Copy(hash, 0, hashBytes, 16, 20);
            return Convert.ToBase64String(hashBytes);
        }
    }
}
