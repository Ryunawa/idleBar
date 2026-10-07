namespace IdleBar.Ui;

public sealed class FloatingGain
{
    public FloatingGain(LaneGain gain, float age)
    {
        Gain = gain;
        Age = age;
    }

    public LaneGain Gain { get; }

    public float Age { get; set; }
}