namespace Wotr.Releases;

// An operator-readable refusal. Whatever raised it published, announced and moved nothing.
public sealed class ReleaseRejected(string message) : Exception(message);
