namespace IdleBar.Scene;

public sealed record SceneLight(float Daylight, float Warmth, float Sun, float Moon, bool Rain, bool Snow)
{
    public static SceneLight Noon { get; } = new(1, 0, 0.5f, -1, false, false);

    public static SceneLight Midnight { get; } = new(0, 0, -1, 0.5f, false, false);

    public bool Night => Daylight < 0.5f;
}
