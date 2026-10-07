namespace IdleBar.Pixel;

public sealed record Walker(PixelSprite Standing, PixelSprite Striding, PixelSprite Passing)
{
    private const float StepsPerSecond = 5f;

    public PixelSprite Pose(bool moving, float time) =>
        !moving ? Standing : (int)(time * StepsPerSecond) % 2 == 0 ? Striding : Passing;
}