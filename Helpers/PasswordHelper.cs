using System;
using System.Security.Cryptography;
using System.Text;
using Enjaz.Services;

namespace Enjaz.Helpers
{
    /// <summary>
    /// مساعد تشفير كلمات المرور
    /// Password encryption helper using SHA256
    /// </summary>
    public static class PasswordHelper
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32;  // 256 bit
        private const int Iterations = 100000;

        /// <summary>
        /// تشفير كلمة المرور باستخدام PBKDF2 مع Salt
        /// Hash password using PBKDF2 with Salt
        /// </summary>
        public static string HashPassword(string password)
        {
            using (var algorithm = new Rfc2898DeriveBytes(
                password,
                SaltSize,
                Iterations,
                HashAlgorithmName.SHA256))
            {
                var key = Convert.ToBase64String(algorithm.GetBytes(KeySize));
                var salt = Convert.ToBase64String(algorithm.Salt);

                // Format: v2:iterations:salt:hash
                return $"v2:{Iterations}:{salt}:{key}";
            }
        }

        /// <summary>
        /// التحقق من تطابق كلمة المرور (يدعم التشفير القديم والجديد)
        /// Verify if password matches the hash (supports legacy and new hashing)
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(hash)) return false;

            // Handle legacy SHA256 hashes (v1)
            if (!hash.StartsWith("v2:"))
            {
                LoggerService.LogInfo("Verifying password using legacy SHA256 hashing.");
                string legacyHash = HashPasswordLegacy(password);
                return legacyHash.Equals(hash, StringComparison.OrdinalIgnoreCase);
            }

            try
            {
                var parts = hash.Split(':');
                if (parts.Length != 4) return false;

                var iterations = int.Parse(parts[1]);
                var salt = Convert.FromBase64String(parts[2]);
                var key = Convert.FromBase64String(parts[3]);

                LoggerService.LogInfo($"Verifying password using PBKDF2 (v2) with {iterations} iterations.");

                using (var algorithm = new Rfc2898DeriveBytes(
                    password,
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256))
                {
                    var keyToCheck = algorithm.GetBytes(KeySize);
                    return CryptographicOperations.FixedTimeEquals(key, keyToCheck);
                }
            }
            catch
            {
                return false;
            }
        }

        private static string HashPasswordLegacy(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
