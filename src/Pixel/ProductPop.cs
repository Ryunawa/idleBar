namespace IdleBar.Pixel;

public readonly record struct ProductPop(int From, float Age)
{
    private const float FallSeconds = 0.3f;
    private const float SettleSeconds = 0.45f;
    private const int FallHeight = 7;

    public static ProductPop None { get; } = new(int.MaxValue, float.MaxValue);

    public bool Sparkling => Age < SettleSeconds;

    public int Lift(int index) => index < From ? 0 : Age switch
    {
        < FallSeconds => (int)(FallHeight * (1 - Age / FallSeconds)),
        < SettleSeconds => 1,
        _ => 0,
    };
}