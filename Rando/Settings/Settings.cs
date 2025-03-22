namespace FiveKnights.Rando;

public class RandoSettings
{
    public bool Enabled { get; set; }
    public WhiteDefenderRequirement WhiteDefenderRequirement { get; set; } = WhiteDefenderRequirement.Vanilla;
    public bool BossRewards { get; set; }
    public bool AbyssalTemple { get; set; }
    public ChampionsCall ChampionCallCompletion { get; set; } = ChampionsCall.Disabled;
    public RandoSettings Clone()
    {
        return new RandoSettings
        {
            Enabled = Enabled,
            WhiteDefenderRequirement = WhiteDefenderRequirement,
            BossRewards = BossRewards,
            AbyssalTemple = AbyssalTemple,
            ChampionCallCompletion = ChampionCallCompletion
        };
    }
}

public enum WhiteDefenderRequirement
{
    Vanilla = 0,
    Randomized = 1,
    NotRequired = 2
}

public enum ChampionsCall
{
    Disabled = 0,
    Enabled = 1,
    Radiant = 2
}