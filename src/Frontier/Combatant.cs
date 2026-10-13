namespace IdleBar.Frontier;

public readonly record struct Combatant(FighterKind Kind, Ally? Ally)
{
    public long AllyId => Ally?.Id ?? 0;
}
