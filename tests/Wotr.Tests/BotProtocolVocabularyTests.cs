using System.Text;
using System.Text.Json.Nodes;
using Wotr.Contracts;
using Wotr.Domain;
using Wotr.Domain.BotProtocol;
using Wotr.Domain.Content;
using Xunit;

namespace Wotr.Tests;

// BOT-225: the seat writes the protocol's base-game vocabulary (vocabulary-0.1.json, MIT, tests/fixtures/wotr-protocol)
// one to one from the server's own content, and its decisions' fingerprints as SPEC §4 defines them.
public sealed class BotProtocolVocabularyTests
{
    private static readonly JsonObject Vocabulary = (JsonObject)JsonNode.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Protocol", "vocabulary-0.1.json")))!;
    private static readonly BoardData Board = BoardCatalog.LoadBase();

    private static HashSet<string> Keys(string list) =>
        ((JsonArray)Vocabulary[list]!).Select(e => e is JsonObject o ? (string)o["key"]! : (string)e!).ToHashSet();

    [Fact]
    public void EveryRegionIsOneVocabularyRegionWithTheSameNeighbours()
    {
        var theirs = ((JsonArray)Vocabulary["regions"]!).OfType<JsonObject>().ToDictionary(r => (string)r["key"]!);
        var ours = Board.Regions.ToDictionary(r => ProtocolVocabulary.Region(r.Id));
        Assert.Equal(theirs.Keys.Order(), ours.Keys.Order());
        foreach (var (key, region) in ours)
        {
            var neighbours = ((JsonArray)theirs[key]["neighbours"]!).Select(n => (string)n!).Order();
            Assert.Equal(neighbours, region.Neighbors.Select(ProtocolVocabulary.Region).Order());
            Assert.Equal((string?)theirs[key]["nation"], region.Nation is null ? null : ProtocolVocabulary.Nation(region.Nation));
        }
    }

    [Fact]
    public void NationsCharactersFacesAndUnitsAreTheVocabularys()
    {
        Assert.Equal(Keys("nations").Order(), Board.Nations.Select(n => ProtocolVocabulary.Nation(n.Id)).Order());
        Assert.Equal(Keys("characters").Order(), CharacterCatalog.Base.Keys.Select(ProtocolVocabulary.Character).Order());
        var faces = new[] { DieFace.Character, DieFace.Army, DieFace.Muster, DieFace.ArmyMuster, DieFace.Event, DieFace.WillOfTheWest, DieFace.Eye };
        Assert.Equal(Keys("die_faces").Order(), faces.Select(ProtocolVocabulary.Face).Order());
        Assert.Equal(Keys("units").Order(), Enum.GetValues<UnitKind>().Select(ProtocolVocabulary.Unit).Order());
        Assert.Equal(new[] { "FREE_PEOPLES", "SHADOW" }, new[] { Side.FreePeoples, Side.Shadow }.Select(ProtocolVocabulary.Side));
    }

    [Fact]
    public void EveryBaseCardIsItsVocabularyKeyWithItsPrintedTitle()
    {
        var theirs = ((JsonArray)Vocabulary["cards"]!).OfType<JsonObject>().ToDictionary(c => (string)c["key"]!, c => (string)c["title"]!);
        Assert.Equal(96, CardCatalog.Base.Count);
        Assert.Equal(theirs.Keys.Order(), CardCatalog.Base.Keys.Select(ProtocolVocabulary.Card).Order());
        foreach (var card in CardCatalog.Base.Values)
        {
            var title = theirs[ProtocolVocabulary.Card(card.Id)];
            // The vocabulary prints "Swords of Eriador"; the card reads "Swords in Eriador".
            if (title != "Swords of Eriador")
                Assert.Equal(ProtocolVocabulary.Slug(title), ProtocolVocabulary.Slug(card.Title));
        }
    }

    [Fact]
    public void EveryHuntTileIsOneVocabularyTile()
    {
        var ours = Board.HuntTiles.Select(t => ProtocolVocabulary.HuntTile(t.Id)).ToList();
        Assert.Equal(ours.Count, ours.Distinct().Count());
        Assert.Equal(Keys("hunt_tiles").Order(), ours.Order());
        Assert.Equal("shelobs-lair", ProtocolVocabulary.HuntTile("base.hunt.special-sh.die.0.1"));
        Assert.Equal("smeagol-helps-nice-master", ProtocolVocabulary.HuntTile("base.hunt.special-fp.-1.0.1"));
        Assert.Equal("2r", ProtocolVocabulary.HuntTile("base.hunt.standard.2.1.1"));
        Assert.Equal("1r-2", ProtocolVocabulary.HuntTile("base.hunt.standard.1.1.2"));
    }

    [Fact]
    public void TheFingerprintIsFnv1aOverTheDecisionWithoutTexts()
    {
        Assert.Equal(0xcbf29ce484222325UL, ProtocolJson.Fnv1a(Array.Empty<byte>()));
        Assert.Equal(0xaf63dc4c8601ec8cUL, ProtocolJson.Fnv1a(Encoding.UTF8.GetBytes("a")));
        var options = new List<JsonObject>
        {
            new() { ["o"] = 0, ["kind"] = "field", ["text"] = "Fight a field battle" },
            new() { ["o"] = 1, ["kind"] = "siege", ["text"] = "Withdraw" }
        };
        var expected = ProtocolJson.Fnv1a(Encoding.UTF8.GetBytes(
            "SHADOW\nFIELD_OR_SIEGE\n2\n{\"o\":0,\"kind\":\"field\"}\n{\"o\":1,\"kind\":\"siege\"}\n")).ToString("x16");
        Assert.Equal(expected, ProtocolJson.Fingerprint("SHADOW", "FIELD_OR_SIEGE", options));
        Assert.Equal(16, expected.Length);
    }
}
