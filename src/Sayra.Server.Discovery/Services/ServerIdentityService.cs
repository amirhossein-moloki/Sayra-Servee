using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Sayra.Server.Persistence;
using Sayra.Server.Persistence.Entities;

namespace Sayra.Server.Discovery.Services;

public interface IServerIdentityService
{
    Task<ServerIdentityEntity> GetOrCreateIdentityAsync();
    string SignData(string data, string privateKeyPem);
    bool VerifySignature(string data, string signature, string publicKeyPem);
}

public class ServerIdentityService : IServerIdentityService
{
    private readonly IDbContextFactory<SayraDbContext> _contextFactory;
    private ServerIdentityEntity? _cachedIdentity;

    public ServerIdentityService(IDbContextFactory<SayraDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<ServerIdentityEntity> GetOrCreateIdentityAsync()
    {
        if (_cachedIdentity != null) return _cachedIdentity;

        using var context = await _contextFactory.CreateDbContextAsync();
        var identity = await context.ServerIdentities.FirstOrDefaultAsync();

        if (identity == null)
        {
            identity = CreateNewIdentity();
            context.ServerIdentities.Add(identity);
            await context.SaveChangesAsync();
        }

        _cachedIdentity = identity;
        return identity;
    }

    private ServerIdentityEntity CreateNewIdentity()
    {
        using var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportPkcs8PrivateKeyPem();
        var publicKey = rsa.ExportSubjectPublicKeyInfoPem();

        var publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
        var fingerprint = Convert.ToBase64String(SHA256.HashData(publicKeyBytes));

        return new ServerIdentityEntity
        {
            Id = Guid.NewGuid().ToString(),
            ServerName = Environment.MachineName,
            PrivateKey = privateKey,
            PublicKey = publicKey,
            PublicKeyFingerprint = fingerprint,
            CreatedAt = DateTime.UtcNow
        };
    }

    public string SignData(string data, string privateKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return Convert.ToBase64String(signatureBytes);
    }

    public bool VerifySignature(string data, string signature, string publicKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var signatureBytes = Convert.FromBase64String(signature);
        return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    }
}
