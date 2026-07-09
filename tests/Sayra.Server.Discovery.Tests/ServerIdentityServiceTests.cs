using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Sayra.Server.Discovery.Services;
using Sayra.Server.Persistence;
using Sayra.Server.Persistence.Entities;
using Xunit;
using Moq;

namespace Sayra.Server.Discovery.Tests;

public class ServerIdentityServiceTests
{
    [Fact]
    public async Task GetOrCreateIdentityAsync_ShouldCreateNewIdentity_WhenNoneExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SayraDbContext>()
            .UseInMemoryDatabase(databaseName: "DiscoveryTests_Create")
            .Options;
        var factoryMock = new Mock<IDbContextFactory<SayraDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(default)).ReturnsAsync(new SayraDbContext(options));

        var service = new ServerIdentityService(factoryMock.Object);

        // Act
        var identity = await service.GetOrCreateIdentityAsync();

        // Assert
        Assert.NotNull(identity);
        Assert.False(string.IsNullOrEmpty(identity.PrivateKey));
        Assert.False(string.IsNullOrEmpty(identity.PublicKey));
        Assert.False(string.IsNullOrEmpty(identity.PublicKeyFingerprint));
    }

    [Fact]
    public async Task GetOrCreateIdentityAsync_ShouldReturnExistingIdentity_WhenAlreadyExists()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<SayraDbContext>()
            .UseInMemoryDatabase(databaseName: "DiscoveryTests_Existing")
            .Options;

        using (var context = new SayraDbContext(options))
        {
            context.ServerIdentities.Add(new ServerIdentityEntity
            {
                Id = "existing-id",
                ServerName = "ExistingServer",
                PrivateKey = "private",
                PublicKey = "public",
                PublicKeyFingerprint = "fingerprint",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var factoryMock = new Mock<IDbContextFactory<SayraDbContext>>();
        factoryMock.Setup(f => f.CreateDbContextAsync(default)).ReturnsAsync(new SayraDbContext(options));

        var service = new ServerIdentityService(factoryMock.Object);

        // Act
        var identity = await service.GetOrCreateIdentityAsync();

        // Assert
        Assert.Equal("existing-id", identity.Id);
    }

    [Fact]
    public void SignAndVerify_ShouldWorkCorrectly()
    {
        // Arrange
        var factoryMock = new Mock<IDbContextFactory<SayraDbContext>>();
        var service = new ServerIdentityService(factoryMock.Object);

        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        var data = "test-data";

        // Act
        var signature = service.SignData(data, privateKey);
        var isValid = service.VerifySignature(data, signature, publicKey);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Verify_ShouldFail_WithModifiedData()
    {
        // Arrange
        var factoryMock = new Mock<IDbContextFactory<SayraDbContext>>();
        var service = new ServerIdentityService(factoryMock.Object);

        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();
        var data = "test-data";

        // Act
        var signature = service.SignData(data, privateKey);
        var isValid = service.VerifySignature(data + "modified", signature, publicKey);

        // Assert
        Assert.False(isValid);
    }
}
