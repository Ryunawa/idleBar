using System;

namespace IdleBar.Ui;

public sealed class LaneSpeech
{
    public LaneSpeech(Guid author, string name, string text)
    {
        Author = author;
        Name = name;
        Text = text;
    }

    public Guid Author { get; }

    public string Name { get; }

    public string Text { get; }

    public float Age { get; set; }

    public string? Shown { get; set; }

    public int ShownWidth { get; set; }

    public float ShownScale { get; set; }
}
