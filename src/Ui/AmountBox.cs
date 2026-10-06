using Godot;

namespace IdleBar.Ui;

public static class AmountBox
{
    public static SpinBox Create(int initial, int maximum)
    {
        SpinBox box = new()
        {
            MinValue = 1,
            MaxValue = maximum,
            Step = 1,
            Rounded = true,
            Value = initial,
            CustomMinimumSize = new Vector2(84, 0),
            SelectAllOnFocus = true,
            UpdateOnTextChanged = true,
        };
        return box;
    }
}
