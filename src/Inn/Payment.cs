namespace IdleBar.Inn;

public sealed record Payment(int Amount, int X, bool Perfect, Drink? Drink, string? Regular = null);
