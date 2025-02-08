using System.Runtime.CompilerServices;
using SkibidiSteamLogin.Core.Helpers;

namespace SkibidiSteamLogin.Core.Tests.Helpers
{
    public class HexDecoderTests
    {
        [Fact]
        public void Decode_ValidHexString_ReturnsCorrectByteArray()
        {
            // Arrange
            string hex = "4A6F686E";
            byte[] expected = { 0x4A, 0x6F, 0x68, 0x6E };

            // Act
            var result = HexDecoder.Decode(hex);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Decode_EmptyString_ThrowsArgumentException()
        {
            // Arrange
            string hex = "";

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => HexDecoder.Decode(hex));
            Assert.Equal("Input cannot be null or empty. (Parameter 'hex')", ex.Message);
        }

        [Fact]
        public void Decode_NullString_ThrowsArgumentException()
        {
            // Arrange
            string hex = null;

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => HexDecoder.Decode(hex));
            Assert.Equal("Input cannot be null or empty. (Parameter 'hex')", ex.Message);
        }

        [Fact]
        public void Decode_HexStringWithOddLength_ThrowsFormatException()
        {
            // Arrange
            string hex = "4A6F6";

            // Act & Assert
            var ex = Assert.Throws<FormatException>(() => HexDecoder.Decode(hex));
            Assert.Equal("Hexadecimal string must have an even length.", ex.Message);
        }

        [Fact]
        public void Decode_InvalidHexCharacters_ThrowsFormatException()
        {
            // Arrange
            string hex = "4G6Z";

            // Act & Assert
            var ex = Assert.Throws<FormatException>(() => HexDecoder.Decode(hex));
            Assert.Equal("Invalid hexadecimal character: G", ex.Message);
        }

        [Fact]
        public void Decode_ValidHexStringWithLowercase_ReturnsCorrectByteArray()
        {
            // Arrange
            string hex = "4a6f686e";
            byte[] expected = { 0x4A, 0x6F, 0x68, 0x6E };

            // Act
            var result = HexDecoder.Decode(hex);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Decode_AllZeroes_ReturnsCorrectByteArray()
        {
            // Arrange
            string hex = "00000000";
            byte[] expected = { 0x00, 0x00, 0x00, 0x00 };

            // Act
            var result = HexDecoder.Decode(hex);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Decode_SingleByteHexString_ReturnsCorrectByteArray()
        {
            // Arrange
            string hex = "0A";
            byte[] expected = { 0x0A };

            // Act
            var result = HexDecoder.Decode(hex);

            // Assert
            Assert.Equal(expected, result);
        }
    }
}
