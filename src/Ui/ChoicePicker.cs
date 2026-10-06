using System.Collections.Generic;
using Godot;

namespace IdleBar.Ui;

public sealed class ChoicePicker
{
    private readonly List<PickerChoice> _choices = [];

    public ChoicePicker(float minimumWidth)
    {
        Button = new OptionButton
        {
            FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(minimumWidth, 0),
            ClipText = true,
            FitToLongestItem = false,
        };
    }

    public OptionButton Button { get; }

    public bool HasSelection => Button.Selected >= 0 && Button.Selected < _choices.Count;

    public string? SelectedId => HasSelection ? _choices[Button.Selected].Id : null;

    public void Choose(string? id)
    {
        int index = _choices.FindIndex(choice => choice.Id == id);
        if (index >= 0 && index != Button.Selected)
        {
            Button.Select(index);
        }
    }

    public void Fill(IReadOnlyList<PickerChoice> choices)
    {
        bool hadSelection = HasSelection;
        string? previous = SelectedId;
        _choices.Clear();
        _choices.AddRange(choices);
        Button.Clear();
        foreach (PickerChoice choice in choices)
        {
            Button.AddItem(choice.Label);
        }

        int index = hadSelection ? _choices.FindIndex(choice => choice.Id == previous) : -1;
        Button.Select(index >= 0 ? index : choices.Count > 0 ? 0 : -1);
        Button.Disabled = choices.Count == 0;
    }
}
