using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using SkibidiSteamLogin.Core.Models.SteamResponses;

[assembly: InternalsVisibleTo("SkibidiSteamLogin.Core.Tests")]
namespace SkibidiSteamLogin.Core.Helpers
{
    internal static class EncryptionHelper
    {
        internal static string EncryptPassword(RsaData rsaData, string password)
        {
            if (string.IsNullOrWhiteSpace(rsaData.Modulus) || string.IsNullOrWhiteSpace(rsaData.Exponent))
                throw new ArgumentNullException("Public key modulus and exponent cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException("Password cannot be null or empty.");

            using var rsa = new RSACryptoServiceProvider();

            var rsaParams = new RSAParameters()
            {
                Modulus = HexDecoder.Decode(rsaData.Modulus),
                Exponent = HexDecoder.Decode(rsaData.Exponent ),
            };

            rsa.ImportParameters(rsaParams);

            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var encryptedBytes = rsa.Encrypt(passwordBytes, RSAEncryptionPadding.Pkcs1);

            return Convert.ToBase64String(encryptedBytes);
        }

    }
}
