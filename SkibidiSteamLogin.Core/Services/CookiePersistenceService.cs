using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkibidiSteamLogin.Core.Interfaces;

namespace SkibidiSteamLogin.Core.Services
{
    internal class CookiePersistenceService : ICookiePersistenceService
    {
        private const string CookieFilePath = "cookies.dat";
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("SkibidiSteamLogin_CookieSalt_v1");
        private readonly ILogger<CookiePersistenceService> _logger;

        public CookiePersistenceService(ILogger<CookiePersistenceService> logger)
        {
            _logger = logger;
        }

        public async Task SaveCookiesAsync(CookieCollection cookies)
        {
            var serialized = JsonConvert.SerializeObject(cookies);
            var encrypted = Encrypt(serialized);
            await File.WriteAllBytesAsync(CookieFilePath, encrypted);
            _logger.LogInformation("Cookies saved successfully.");
        }

        public async Task<CookieCollection> LoadCookiesAsync()
        {
            if (!File.Exists(CookieFilePath))
            {
                _logger.LogWarning("Cookie file not found at {Path}.", CookieFilePath);
                return new CookieCollection();
            }

            var encrypted = await File.ReadAllBytesAsync(CookieFilePath);
            var decrypted = Decrypt(encrypted);
            var cookies = JsonConvert.DeserializeObject<CookieCollection>(decrypted);
            _logger.LogInformation("Cookies loaded successfully.");
            return cookies ?? new CookieCollection();
        }

        private static byte[] DeriveKey()
        {
            var keyMaterial = Encoding.UTF8.GetBytes(Environment.MachineName);
            using var kdf = new Rfc2898DeriveBytes(keyMaterial, Salt, 100_000, HashAlgorithmName.SHA256);
            return kdf.GetBytes(32);
        }

        private static byte[] Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey();
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + cipherBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
            return result;
        }

        private static string Decrypt(byte[] encryptedData)
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey();

            var iv = new byte[aes.BlockSize / 8];
            Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var cipherBytes = new byte[encryptedData.Length - iv.Length];
            Buffer.BlockCopy(encryptedData, iv.Length, cipherBytes, 0, cipherBytes.Length);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
