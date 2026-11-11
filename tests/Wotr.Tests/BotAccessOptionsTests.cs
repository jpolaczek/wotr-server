using Microsoft.Extensions.Configuration;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public class BotAccessOptionsTests
{
    [Fact]
    public void ConfigurationDefaultsToDisabledAndRejectsInvalidIdentityOrCoverage()
    {
        var defaults = BotAccessOptions.Read(new ConfigurationBuilder().Build());
        Assert.False(defaults.Enabled);
        Assert.True(defaults.Supports(new()));
        Assert.False(defaults.Supports(new() { Council = true }));
        Assert.False(defaults.Supports(new() { Kings = true, VisibleCorruption = true }));
        (string Key, string Value)[] invalid =
        [
            ("ServiceId", "invalid/service"), ("ControlTokenHash", "not-a-hash"), ("MinimumVersion", "latest"),
            ("SeatLifetimeSeconds", "1"), ("SeatLifetimeSeconds", "901"), ("Features", "fate"), ("Features", "base,unknown")
        ];
        foreach (var (key, value) in invalid)
        {
            var settings = new Dictionary<string, string?>
            {
                ["Wotr:Bots:Enabled"] = "true", ["Wotr:Bots:ServiceId"] = "fixture",
                ["Wotr:Bots:ControlTokenHash"] = new string('A', 64), ["Wotr:Bots:" + key] = value
            };
            Assert.Throws<InvalidOperationException>(() => BotAccessOptions.Read(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        }
    }
}
