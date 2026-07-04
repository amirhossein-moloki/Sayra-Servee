using Sayra.Server.Shared.Messages;

namespace Sayra.Server.Security;

public interface ISecureMessageValidator
{
    bool Validate(SecureEnvelope envelope, string sessionKey);
}

public class SecureMessageValidator : ISecureMessageValidator
{
    private readonly ISignatureService _signatureService;
    private readonly IReplayProtectionService _replayProtectionService;

    public SecureMessageValidator(ISignatureService signatureService, IReplayProtectionService replayProtectionService)
    {
        _signatureService = signatureService;
        _replayProtectionService = replayProtectionService;
    }

    public bool Validate(SecureEnvelope envelope, string sessionKey)
    {
        if (!_replayProtectionService.IsValid(envelope.ClientId, envelope.Nonce, envelope.Timestamp))
        {
            return false;
        }

        string dataToVerify = $"{envelope.Payload}:{envelope.Timestamp}:{envelope.Nonce}:{envelope.ClientId}";
        return _signatureService.Verify(dataToVerify, envelope.Signature, sessionKey);
    }
}
