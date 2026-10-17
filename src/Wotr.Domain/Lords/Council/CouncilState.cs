namespace Wotr.Domain;

public sealed class CouncilState
{
    public bool HomelandsChosen { get; set; }
    public List<string> GrantedTokens { get; set; } = [];
    public List<string> SpentTokens { get; set; } = [];
    public int LastTokenTurn { get; set; } = -1;
    public int LastFellowshipDieTurn { get; set; } = -1;
    public string FellowshipBoxDieId { get; set; } = "";
    public List<CompanionBattleReturn> BattleReturns { get; set; } = [];
}
