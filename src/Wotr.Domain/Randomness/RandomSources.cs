using System.Text.Json;
using Wotr.Contracts;
using Wotr.Domain.Content;

namespace Wotr.Domain;

public interface IRandomSource
{
    int Next(int exclusiveMaximum);
}

// Used only in tests/replays. Production injects cryptographic entropy in the server.

public sealed class SequenceRandom(params int[] values) : IRandomSource
{
    private int index;
    public int Calls => index;
    public int Next(int exclusiveMaximum)
    {
        if (index >= values.Length)
            throw new InvalidOperationException("Test random sequence exhausted.");
        var value = values[index++];
        if (value < 0 || value >= exclusiveMaximum)
            throw new InvalidOperationException("Test random sample outside range.");
        return value;
    }
}

public sealed class RecordingRandom(IRandomSource source, MatchState state) : IRandomSource
{
    public int Next(int exclusiveMaximum)
    {
        var value = source.Next(exclusiveMaximum);
        state.RandomSamples.Add(new()
        {
            Revision = state.Revision,
            ExclusiveMaximum = exclusiveMaximum,
            Value = value
        });
        return value;
    }
}
