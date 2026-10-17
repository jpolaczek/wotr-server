namespace Wotr.Releases;

// manifest.json.sig: the Ed25519 signature of the exact manifest bytes and the key that made it.
public sealed class ManifestSignature
{
    public const string Ed25519 = "Ed25519";

    public string Algorithm { get; set; } = Ed25519;
    public string KeyId { get; set; } = "";
    public string Signature { get; set; } = "";

    public static byte[] Create(byte[] manifest, ReleasePrivateKey key) => ReleaseJson.Serialize(new ManifestSignature
    {
        KeyId = key.PublicKey.KeyId,
        Signature = Convert.ToBase64String(key.Sign(manifest))
    });

    public static void Verify(byte[] manifest, byte[] signatureFile, ReleasePublicKey key, string description)
    {
        var signature = ReleaseJson.Parse<ManifestSignature>(signatureFile, description + " signature");
        if (signature.Algorithm != Ed25519 || signature.KeyId != key.KeyId)
            throw new ReleaseRejected(description + " is signed by another key.");
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(signature.Signature);
        }
        catch (FormatException)
        {
            throw new ReleaseRejected(description + " signature is not base64.");
        }
        if (!key.Verifies(manifest, bytes))
            throw new ReleaseRejected(description + " signature does not match its bytes.");
    }
}
