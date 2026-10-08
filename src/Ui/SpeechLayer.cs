using System;
using System.Collections.Generic;
using Godot;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public sealed class SpeechLayer
{
    private const int FontSize = 12;
    private const int Padding = 2;
    private const int MinTailWidth = 5;
    private const int Room = 170;
    private const float Shortest = 4f;
    private const float Longest = 10f;
    private const float PerLetter = 0.07f;

    private static readonly Color Fill = new("efe6d2");
    private static readonly Color Edge = new("1a1411");

    private readonly List<LaneSpeech> _speeches = [];

    public void Speak(Guid author, string name, string text)
    {
        _speeches.RemoveAll(speech => speech.Author == author);
        _speeches.Add(new LaneSpeech(author, name, text));
    }

    public void Advance(float delta)
    {
        foreach (LaneSpeech speech in _speeches)
        {
            speech.Age += delta;
        }

        _speeches.RemoveAll(speech => speech.Age > Seconds(speech));
    }

    public void Paint(PixelCanvas canvas, Font font, int top, Func<Guid, int> anchorOf)
    {
        int fontSize = PixelFont.Size(FontSize);
        int height = canvas.TextHeight(font, fontSize) + Padding;
        foreach (LaneSpeech speech in _speeches)
        {
            if (speech.Shown is null || speech.ShownScale != canvas.Scale)
            {
                speech.Shown = Fit(canvas, font, fontSize, speech.Text);
                speech.ShownWidth = canvas.TextWidth(font, fontSize, speech.Shown) + Padding * 2;
                speech.ShownScale = canvas.Scale;
            }

            float alpha = Math.Clamp(Math.Min(speech.Age * 6, (Seconds(speech) - speech.Age) * 3), 0, 1);
            int width = speech.ShownWidth;
            int center = anchorOf(speech.Author);
            int left = Math.Clamp(center - width / 2, 1, Math.Max(1, canvas.Width - width - 1));
            Color fill = Fill with { A = alpha };
            Color edge = Edge with { A = alpha };
            canvas.Fill(left + 1, top, width - 2, height, edge);
            canvas.Fill(left, top + 1, width, height - 2, edge);
            canvas.Fill(left + 1, top + 1, width - 2, height - 2, fill);
            if (width >= MinTailWidth)
            {
                canvas.Fill(Math.Clamp(center, left + 2, left + width - 3), top + height, 1, 1, edge);
            }

            canvas.Text(font, fontSize, speech.Shown, left + Padding, top + 1, edge, fill);
        }
    }

    private static string Fit(PixelCanvas canvas, Font font, int fontSize, string text)
    {
        string shown = text;
        while (shown.Length > 1 && canvas.TextWidth(font, fontSize, shown) > Room)
        {
            int keep = shown.TrimEnd('…').Length - 1;
            if (keep <= 0)
            {
                return "…";
            }

            shown = text[..keep].TrimEnd() + "…";
        }

        return shown;
    }

    private static float Seconds(LaneSpeech speech) => Math.Min(Longest, Shortest + speech.Text.Length * PerLetter);
}
