namespace IdleBar.Frontier;

public sealed class Monster
{
    public Monster(long id, int template, bool boss, int stage, double hp, double approach, double bossSeconds = Balance.BossSeconds)
    {
        Id = id;
        Template = template;
        Boss = boss;
        Stage = stage;
        MaxHp = hp;
        Hp = hp;
        Approach = approach;
        ApproachTotal = approach;
        BossLeft = boss ? bossSeconds : 0;
        BossTotal = BossLeft;
    }

    public long Id { get; }

    public int Template { get; }

    public bool Boss { get; }

    public int Stage { get; }

    public double MaxHp { get; }

    public double Hp { get; set; }

    public double Approach { get; set; }

    public double ApproachTotal { get; }

    public double BossLeft { get; set; }

    public double BossTotal { get; }

    public string Name => Biomes.NameOf(this);
}
