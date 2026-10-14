using IdleBar.Frontier;

namespace IdleBar.Scene;

public sealed record Stance(HeroRole Role, HeroFigure Figure, int X, float Swing, bool Hurt, bool Down, double Health, double Revival);
