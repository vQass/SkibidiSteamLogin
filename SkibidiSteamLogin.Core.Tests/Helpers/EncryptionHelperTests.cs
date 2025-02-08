using SkibidiSteamLogin.Core.Helpers;
using SkibidiSteamLogin.Core.Models.SteamResponses;

namespace SkibidiSteamLogin.Core.Tests.Helpers
{
    public class EncryptionHelperTests
    {
        [Fact]
        public void EncryptPassword_ValidInputs_ReturnsBase64String()
        {
            // Arrange
            string publicKeyModulus = "b5c0fc3ec7f65db2b80c612be9ccae0248ff1b22c47080a7b8d82744c6981755ab7a91d23bf7562168ebdb827ff2021c9efb91cc762f6f0e5f0a6d8504c940c32ff451f63efe21b94eca606c0652cc3d15c95cf2ddf448ddbf1d7ff2cbcf23535d20d15e9ccd3b58db425f23c3b1c8f59bd3e3888dea79fab46ce0244068a147c0e8865137c7ccd50772af90a34e65105bc833daa00e15fc2392d4b723bbf95ec71c0e0b808c7ee89191866d14a9882057fd0dce08d0ce4e6522661cd8063b007fb002c66aa524184546e97010707988e40d2f06f7b6c5ef40f8fe4a817925b4b460ce1e45c2bc60031e101e27c936880bd6cb54da12476193693b9b81808b11";
            string publicKeyExponent = "010001";
            string password = "securePassword123";
            var rsaData = new RsaData(publicKeyModulus, publicKeyExponent);

            // Act
            string encryptedPassword = EncryptionHelper.EncryptPassword(rsaData, password);

            // Assert
            Assert.NotNull(encryptedPassword);
            Assert.NotEmpty(encryptedPassword);
            Assert.True(IsBase64String(encryptedPassword));
        }

        [Fact]
        public void EncryptPassword_NullOrEmptyModulus_ThrowsArgumentException()
        {
            // Arrange
            string publicKeyModulus = null; // Invalid modulus
            string publicKeyExponent = "010001";
            string password = "securePassword123";
            var rsaData = new RsaData(publicKeyModulus, publicKeyExponent);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EncryptionHelper.EncryptPassword(rsaData, password));
        }

        [Fact]
        public void EncryptPassword_NullOrEmptyExponent_ThrowsArgumentException()
        {
            // Arrange
            string publicKeyModulus = "b5c0fc3ec7f65db2b80c612be9ccae0248ff1b22c47080a7b8d82744c6981755ab7a91d23bf7562168ebdb827ff2021c9efb91cc762f6f0e5f0a6d8504c940c32ff451f63efe21b94eca606c0652cc3d15c95cf2ddf448ddbf1d7ff2cbcf23535d20d15e9ccd3b58db425f23c3b1c8f59bd3e3888dea79fab46ce0244068a147c0e8865137c7ccd50772af90a34e65105bc833daa00e15fc2392d4b723bbf95ec71c0e0b808c7ee89191866d14a9882057fd0dce08d0ce4e6522661cd8063b007fb002c66aa524184546e97010707988e40d2f06f7b6c5ef40f8fe4a817925b4b460ce1e45c2bc60031e101e27c936880bd6cb54da12476193693b9b81808b11";
            string publicKeyExponent = null; // Invalid exponent
            string password = "securePassword123";
            var rsaData = new RsaData(publicKeyModulus, publicKeyExponent);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EncryptionHelper.EncryptPassword(rsaData, password));
        }

        [Fact]
        public void EncryptPassword_NullOrEmptyPassword_ThrowsArgumentNullException()
        {
            // Arrange
            string publicKeyModulus = "b5c0fc3ec7f65db2b80c612be9ccae0248ff1b22c47080a7b8d82744c6981755ab7a91d23bf7562168ebdb827ff2021c9efb91cc762f6f0e5f0a6d8504c940c32ff451f63efe21b94eca606c0652cc3d15c95cf2ddf448ddbf1d7ff2cbcf23535d20d15e9ccd3b58db425f23c3b1c8f59bd3e3888dea79fab46ce0244068a147c0e8865137c7ccd50772af90a34e65105bc833daa00e15fc2392d4b723bbf95ec71c0e0b808c7ee89191866d14a9882057fd0dce08d0ce4e6522661cd8063b007fb002c66aa524184546e97010707988e40d2f06f7b6c5ef40f8fe4a817925b4b460ce1e45c2bc60031e101e27c936880bd6cb54da12476193693b9b81808b11";
            string publicKeyExponent = "010001";
            string password = null; // Invalid password
            var rsaData = new RsaData(publicKeyModulus, publicKeyExponent);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => EncryptionHelper.EncryptPassword(rsaData, password));
        }

        [Fact]
        public void EncryptPassword_InvalidModulus_ThrowsFormatException()
        {
            // Arrange
            string publicKeyModulus = "INVALID_BASE64"; // Invalid Base64
            string publicKeyExponent = "010001";
            string password = "securePassword123";
            var rsaData = new RsaData(publicKeyModulus, publicKeyExponent);

            // Act & Assert
            Assert.Throws<FormatException>(() => EncryptionHelper.EncryptPassword(rsaData, password));
        }

        // Helper method to validate Base64 strings
        private bool IsBase64String(string input)
        {
            Span<byte> buffer = new Span<byte>(new byte[input.Length]);
            return Convert.TryFromBase64String(input, buffer, out _);
        }
    }
}
