using Microsoft.Extensions.Configuration;
using Wotr.Server;
using Xunit;

namespace Wotr.Tests;

public sealed class BotRoomOptionsTests
{
    [Fact]
    public void AdmissionIsOffUntilAnAcceptedPolicyVersionIsConfigured()
    {
        Assert.False(BotRoomOptions.Read(new ConfigurationBuilder().Build()).Enabled);
        var settings = new Dictionary<string, string?> { ["Wotr:Bots:RoomsEnabled"] = "true" };
        Assert.Throws<InvalidOperationException>(() => BotRoomOptions.Read(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        settings["Wotr:Bots:AcceptedPolicyVersion"] = "reviewed-1.0.0";
        Assert.True(BotRoomOptions.Read(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()).Enabled);
    }
}
