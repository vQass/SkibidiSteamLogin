using SkibidiSteamLogin.Core.Enums;
using SkibidiSteamLogin.Core.Mapping;
using SkibidiSteamLogin.Core.Models.SteamResponses;

namespace SkibidiSteamLogin.Core.Tests.Mapping
{
    public class LoginResultMappingTests
    {
        [Fact]
        public void ToLoginResult_ValidResponse_MapsAllFields()
        {
            var allowedConfirmations = new List<AllowedConfirmation>
            {
                new() { ConfirmationType = AuthGuardType.EmailCode, AssociatedMessage = "test@email.com" }
            };

            var response = new SteamLoginResponse
            {
                ClientId = "123",
                SteamId = "456",
                RequestId = "789",
                AllowedConfirmations = allowedConfirmations
            };

            var result = response.ToLoginResult();

            Assert.Equal("123", result.ClientId);
            Assert.Equal("456", result.SteamId);
            Assert.Equal("789", result.RequestId);
            Assert.Equal(allowedConfirmations, result.AllowedConfirmation);
        }

        [Fact]
        public void ToLoginResult_NullFields_MapsNulls()
        {
            var response = new SteamLoginResponse
            {
                ClientId = null,
                SteamId = null,
                RequestId = null,
                AllowedConfirmations = null
            };

            var result = response.ToLoginResult();

            Assert.Null(result.ClientId);
            Assert.Null(result.SteamId);
            Assert.Null(result.RequestId);
            Assert.Null(result.AllowedConfirmation);
        }

        [Fact]
        public void ToLoginResult_EmptyConfirmations_MapsEmptyList()
        {
            var response = new SteamLoginResponse
            {
                ClientId = "1",
                SteamId = "2",
                RequestId = "3",
                AllowedConfirmations = new List<AllowedConfirmation>()
            };

            var result = response.ToLoginResult();

            Assert.NotNull(result.AllowedConfirmation);
            Assert.Empty(result.AllowedConfirmation);
        }

        [Fact]
        public void ToLoginResult_MultipleConfirmations_MapsAll()
        {
            var confirmations = new List<AllowedConfirmation>
            {
                new() { ConfirmationType = AuthGuardType.EmailCode, AssociatedMessage = "email" },
                new() { ConfirmationType = AuthGuardType.DeviceCode, AssociatedMessage = "device" },
                new() { ConfirmationType = AuthGuardType.DeviceConfirmation, AssociatedMessage = "confirm" }
            };

            var response = new SteamLoginResponse
            {
                ClientId = "1",
                SteamId = "2",
                RequestId = "3",
                AllowedConfirmations = confirmations
            };

            var result = response.ToLoginResult();

            Assert.Equal(3, result.AllowedConfirmation.Count);
            Assert.Equal(AuthGuardType.EmailCode, result.AllowedConfirmation[0].ConfirmationType);
            Assert.Equal(AuthGuardType.DeviceCode, result.AllowedConfirmation[1].ConfirmationType);
            Assert.Equal(AuthGuardType.DeviceConfirmation, result.AllowedConfirmation[2].ConfirmationType);
        }
    }
}
