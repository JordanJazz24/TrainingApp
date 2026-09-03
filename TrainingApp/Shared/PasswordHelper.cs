using System;
using System.Security.Cryptography;

namespace TrainingApp.Shared
{
    public static class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100000;

        public static string Hash(string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);

            byte[] hash = Pbkdf2(password, salt);
            byte[] combined = new byte[SaltSize + HashSize];
            Buffer.BlockCopy(salt, 0, combined, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, combined, SaltSize, HashSize);
            return Convert.ToBase64String(combined);
        }

        public static bool Verify(string password, string storedHash)
        {
            byte[] combined = Convert.FromBase64String(storedHash);
            if (combined.Length != SaltSize + HashSize) return false;

            byte[] salt = new byte[SaltSize];
            byte[] hash = new byte[HashSize];
            Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);
            Buffer.BlockCopy(combined, SaltSize, hash, 0, HashSize);

            byte[] computed = Pbkdf2(password, salt);
            return SlowEquals(hash, computed);
        }

        private static byte[] Pbkdf2(string password, byte[] salt)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                return pbkdf2.GetBytes(HashSize);
        }

        // Constant-time comparison to prevent timing attacks
        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)(a.Length ^ b.Length);
            for (int i = 0; i < a.Length && i < b.Length; i++)
                diff |= (uint)(a[i] ^ b[i]);
            return diff == 0;
        }
    }
}
