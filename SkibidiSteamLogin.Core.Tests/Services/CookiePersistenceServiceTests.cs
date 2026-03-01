using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Moq;
using SkibidiSteamLogin.Core.Services;

namespace SkibidiSteamLogin.Core.Tests.Services
{
    public class CookiePersistenceServiceTests : IDisposable
    {
        private const string CookieFilePath = "cookies.dat";
        private readonly CookiePersistenceService _sut;
        private readonly Mock<ILogger<CookiePersistenceService>> _loggerMock;

        public CookiePersistenceServiceTests()
        {
            _loggerMock = new Mock<ILogger<CookiePersistenceService>>();
            _sut = new CookiePersistenceService(_loggerMock.Object);

            // Clean up before each test
            if (File.Exists(CookieFilePath))
                File.Delete(CookieFilePath);
        }

        public void Dispose()
        {
            if (File.Exists(CookieFilePath))
                File.Delete(CookieFilePath);
        }

        [Fact]
        public async Task SaveAndLoad_RoundTrip_ReturnsSameCookies()
        {
            // Arrange
            var cookies = new CookieCollection
            {
                new Cookie("session", "abc123", "/", "steamcommunity.com"),
                new Cookie("token", "xyz789", "/", "store.steampowered.com")
            };

            // Act
            await _sut.SaveCookiesAsync(cookies);
            var loaded = await _sut.LoadCookiesAsync();

            // Assert
            Assert.Equal(2, loaded.Count);
            for (int i = 0; i < cookies.Count; i++)
            {
                Assert.Equal(cookies[i].Name, loaded[i].Name);
                Assert.Equal(cookies[i].Value, loaded[i].Value);
                Assert.Equal(cookies[i].Domain, loaded[i].Domain);
            }
        }

        [Fact]
        public async Task LoadCookiesAsync_WhenFileDoesNotExist_ReturnsEmptyCookieCollection()
        {
            // Arrange — ensure file doesn't exist
            if (File.Exists(CookieFilePath))
                File.Delete(CookieFilePath);

            // Act
            var result = await _sut.LoadCookiesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task LoadCookiesAsync_WhenFileIsCorrupted_ThrowsCryptographicException()
        {
            // Arrange — write garbage data (at least 16 bytes for IV extraction)
            await File.WriteAllBytesAsync(CookieFilePath, new byte[] {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16,
                99, 99, 99, 99, 99, 99, 99, 99
            });

            // Act & Assert
            await Assert.ThrowsAsync<CryptographicException>(() => _sut.LoadCookiesAsync());
        }

        [Fact]
        public async Task SaveCookiesAsync_EmptyCookieCollection_CanBeLoadedBack()
        {
            // Arrange
            var emptyCookies = new CookieCollection();

            // Act
            await _sut.SaveCookiesAsync(emptyCookies);
            var loaded = await _sut.LoadCookiesAsync();

            // Assert
            Assert.NotNull(loaded);
            Assert.Empty(loaded);
        }

        [Fact]
        public async Task SaveCookiesAsync_NullCookies_LoadReturnsEmptyCollection()
        {
            // Act
            await _sut.SaveCookiesAsync(null);
            var loaded = await _sut.LoadCookiesAsync();

            // Assert
            Assert.NotNull(loaded);
            Assert.Empty(loaded);
        }

        [Fact]
        public async Task LoadCookiesAsync_WhenFileTooShort_ThrowsException()
        {
            // Arrange — data shorter than AES IV length (16 bytes)
            await File.WriteAllBytesAsync(CookieFilePath, new byte[] { 1, 2, 3 });

            // Act & Assert — should throw some exception (ArgumentException from Buffer.BlockCopy or CryptographicException)
            await Assert.ThrowsAnyAsync<Exception>(() => _sut.LoadCookiesAsync());
        }

        [Fact]
        public async Task SaveCookiesAsync_CreatesFileOnDisk()
        {
            // Arrange
            var cookies = new CookieCollection
            {
                new Cookie("test", "value", "/", "example.com")
            };

            // Act
            await _sut.SaveCookiesAsync(cookies);

            // Assert
            Assert.True(File.Exists(CookieFilePath));
            var bytes = await File.ReadAllBytesAsync(CookieFilePath);
            Assert.True(bytes.Length > 0);
        }

        [Fact]
        public async Task SaveCookiesAsync_OverwritesExistingFile()
        {
            // Arrange
            var cookies1 = new CookieCollection
            {
                new Cookie("first", "value1", "/", "example.com")
            };
            var cookies2 = new CookieCollection
            {
                new Cookie("second", "value2", "/", "other.com")
            };

            // Act
            await _sut.SaveCookiesAsync(cookies1);
            await _sut.SaveCookiesAsync(cookies2);
            var loaded = await _sut.LoadCookiesAsync();

            // Assert
            Assert.Single(loaded);
            Assert.Equal("second", loaded[0].Name);
            Assert.Equal("value2", loaded[0].Value);
        }
    }
}
