using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("SkibidiSteamLogin.Core.Tests")]
namespace SkibidiSteamLogin.Core.Helpers
{
    internal static class HexDecoder
    {
        internal static byte[] Decode(string hex)
        {
            if (string.IsNullOrWhiteSpace(hex))
                throw new ArgumentException("Input cannot be null or empty.", nameof(hex));

            if (hex.Length % 2 != 0)
                throw new FormatException("Hexadecimal string must have an even length.");

            var result = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
            {
                int highNibble = GetHexValue(hex[i]);
                int lowNibble = GetHexValue(hex[i + 1]);
                result[i / 2] = (byte)((highNibble << 4) | lowNibble);
            }

            return result;
        }

        private static int GetHexValue(char hex)
        {
            if (hex >= '0' && hex <= '9')
                return hex - '0';
            if (hex >= 'A' && hex <= 'F')
                return hex - 'A' + 10;
            if (hex >= 'a' && hex <= 'f')
                return hex - 'a' + 10;

            throw new FormatException($"Invalid hexadecimal character: {hex}");
        }
    }
}
