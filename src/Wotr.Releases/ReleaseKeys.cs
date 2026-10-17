using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace Wotr.Releases;

// The publisher's Ed25519 public key. Clients pin it; the server and operator tool verify with it.
public sealed class ReleasePublicKey
{
    public const int Length = 32;
    private readonly Ed25519PublicKeyParameters key;

    private ReleasePublicKey(byte[] bytes)
    {
        Bytes = bytes;
        key = new Ed25519PublicKeyParameters(bytes, 0);
        KeyId = Convert.ToHexStringLower(SHA256.HashData(bytes).AsSpan(0, 8));
    }

    public byte[] Bytes { get; }
    public string KeyId { get; }
    public string Encoded => Convert.ToBase64String(Bytes);

    public static ReleasePublicKey Parse(string encoded)
    {
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(encoded.Trim());
        }
        catch (FormatException)
        {
            throw new ReleaseRejected("The release public key is not base64.");
        }
        if (bytes.Length != Length)
            throw new ReleaseRejected("The release public key must be 32 bytes of Ed25519.");
        try
        {
            return new ReleasePublicKey(bytes);
        }
        catch (ArgumentException)
        {
            throw new ReleaseRejected("The release public key is not an Ed25519 point.");
        }
    }

    public bool Verifies(byte[] data, byte[] signature)
    {
        if (signature.Length != Ed25519PrivateKeyParameters.SignatureSize)
            return false;
        var verifier = new Ed25519Signer();
        verifier.Init(false, key);
        verifier.BlockUpdate(data, 0, data.Length);
        return verifier.VerifySignature(signature);
    }

    internal static ReleasePublicKey From(Ed25519PublicKeyParameters key) => new(key.GetEncoded());
}

// The signing key stays on the release machine, outside both repositories and the download host.
public sealed class ReleasePrivateKey
{
    private readonly Ed25519PrivateKeyParameters key;

    private ReleasePrivateKey(byte[] seed)
    {
        key = new Ed25519PrivateKeyParameters(seed, 0);
        PublicKey = ReleasePublicKey.From(key.GeneratePublicKey());
    }

    public ReleasePublicKey PublicKey { get; }

    public static ReleasePrivateKey Generate() => new(RandomNumberGenerator.GetBytes(Ed25519PrivateKeyParameters.KeySize));

    public static ReleasePrivateKey FromSeed(byte[] seed)
    {
        if (seed.Length != Ed25519PrivateKeyParameters.KeySize)
            throw new ReleaseRejected("The release private key must be a 32-byte Ed25519 seed.");
        return new ReleasePrivateKey(seed);
    }

    public static ReleasePrivateKey Load(string path)
    {
        if (!OperatingSystem.IsWindows() && (File.GetUnixFileMode(path) & (UnixFileMode.GroupRead | UnixFileMode.OtherRead)) != 0)
            throw new ReleaseRejected("The release private key must not be readable by other users (chmod 600).");
        try
        {
            return FromSeed(Convert.FromBase64String(File.ReadAllText(path).Trim()));
        }
        catch (FormatException)
        {
            throw new ReleaseRejected("The release private key file is not base64.");
        }
    }

    public void Save(string path)
    {
        var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using var writer = new StreamWriter(path, System.Text.Encoding.ASCII, options);
        writer.WriteLine(Convert.ToBase64String(key.GetEncoded()));
    }

    public byte[] Sign(byte[] data)
    {
        var signer = new Ed25519Signer();
        signer.Init(true, key);
        signer.BlockUpdate(data, 0, data.Length);
        return signer.GenerateSignature();
    }
}
