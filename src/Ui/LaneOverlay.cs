using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using IdleBar.Inn;
using IdleBar.Pixel;

namespace IdleBar.Ui;

public sealed class LaneOverlay
{
    private const int FontSize = 12;
    private const float PopSeconds = 1.8f;
    private const float PopRise = 1.5f;
    private const float BannerFade = 0.4f;
    private const int NameFontSize = 12;
    private const int NameRoom = 64;
    private const int NameGap = 3;
    private const int HeartWidth = 4;

    private const int SpeechPadding = 2;
    private const int SpeechRoom = 170;
    private const float SpeechShortest = 4f;
    private const float SpeechLongest = 10f;
    private const float SpeechPerLetter = 0.07f;

    private static readonly Color PlainPop = new("efe6d2");
    private static readonly Color SpeechFill = new("efe6d2");
    private static readonly Color SpeechEdge = new("1a1411");

    private readonly List<LanePop> _pops = [];
    private readonly List<LaneSpeech> _speeches = [];
    private readonly Queue<(string Text, float Seconds)> _banners = new();
    private float _bannerAge;

    public void Pay(Payment payment) => _pops.Add(new LanePop($"+{payment.Amount}", payment.X, PopColor(payment)));

    public void Pop(string text, int x, Color color) => _pops.Add(new LanePop(text, x, color));

    public void Announce(string text, float seconds)
    {
        if (_banners.Count == 0)
        {
            _bannerAge = 0;
        }

        _banners.Enqueue((text, seconds));
    }

    public void Advance(float delta)
    {
        foreach (LanePop pop in _pops)
        {
            pop.Age += delta;
        }

        _pops.RemoveAll(pop => pop.Age > PopSeconds);
        foreach (LaneSpeech speech in _speeches)
        {
            speech.Age += delta;
        }

        _speeches.RemoveAll(speech => speech.Age > SpeechSeconds(speech));
        if (_banners.Count == 0)
        {
            return;
        }

        _bannerAge += delta;
        if (_bannerAge >= _banners.Peek().Seconds)
        {
            _banners.Dequeue();
            _bannerAge = 0;
        }
    }

    public void Paint(PixelCanvas canvas, Font font, int top, int bannerCenter)
    {
        int fontSize = PixelFont.Size(FontSize);
        foreach (LanePop pop in _pops)
        {
            float alpha = Math.Clamp(Math.Min(pop.Age * 6, (PopSeconds - pop.Age) * 2), 0, 1);
            int x = Math.Max(1, pop.X - canvas.TextWidth(font, fontSize, pop.Text) / 2);
            int y = top + 2 - (int)(pop.Age * PopRise);
            canvas.Text(font, fontSize, pop.Text, x, y, pop.Color with { A = alpha }, BarPalette.Shadow with { A = alpha * 0.9f });
        }

        if (_banners.Count == 0)
        {
            return;
        }

        (string text, float seconds) = _banners.Peek();
        float fade = Math.Clamp(Math.Min(_bannerAge, seconds - _bannerAge) / BannerFade, 0, 1);
        int width = canvas.TextWidth(font, fontSize, text);
        int left = Math.Clamp(bannerCenter - width / 2, 1, Math.Max(1, canvas.Width - width - 1));
        canvas.Text(font, fontSize, text, left, top + 1, BarPalette.Gold with { A = fade }, BarPalette.Shadow with { A = fade * 0.9f });
    }

    public static void PaintNames(PixelCanvas canvas, Font font, int top, IReadOnlyList<Patron> patrons, Func<Patron, NameTag?> tagOf)
    {
        List<(Patron Patron, NameTag Tag)> tagged = patrons
            .Where(patron => patron.Phase != PatronPhase.Gone)
            .Select(patron => (patron, tagOf(patron)))
            .Where(pair => pair.Item2 is not null)
            .Select(pair => (pair.patron, pair.Item2!))
            .ToList();
        foreach ((Patron patron, NameTag tag) in tagged)
        {
            float nearest = tagged.Where(other => other.Patron != patron).Select(other => Math.Abs(other.Patron.X - patron.X)).DefaultIfEmpty(NameRoom).Min();
            PaintTag(canvas, font, top, (int)MathF.Round(patron.X), tag, (int)Math.Min(NameRoom, nearest - NameGap));
        }
    }

    public static void PaintTag(PixelCanvas canvas, Font font, int top, int center, NameTag tag, int room = NameRoom)
    {
        int heart = tag.Heart ? HeartWidth : 0;
        string shown = Fit(canvas, font, tag.Text, room - heart);
        int width = canvas.TextWidth(font, NameFontSize, shown) + heart;
        int x = Math.Clamp(center - width / 2, 1, Math.Max(1, canvas.Width - width - 1));
        if (tag.Heart)
        {
            PatronPainter.PaintHeart(canvas, x, top + 2);
        }

        canvas.Text(font, NameFontSize, shown, x + heart, top + 1, tag.Color, BarPalette.Shadow);
    }

    public void Speak(Guid author, string name, string text)
    {
        _speeches.RemoveAll(speech => speech.Author == author);
        _speeches.Add(new LaneSpeech(author, name, text));
    }

    public void PaintSpeech(PixelCanvas canvas, Font font, int top, Func<Guid, int> anchorOf)
    {
        int fontSize = PixelFont.Size(FontSize);
        int height = canvas.TextHeight(font, fontSize) + SpeechPadding;
        foreach (LaneSpeech speech in _speeches)
        {
            float alpha = Math.Clamp(Math.Min(speech.Age * 6, (SpeechSeconds(speech) - speech.Age) * 3), 0, 1);
            string shown = FitSpeech(canvas, font, fontSize, speech.Text);
            int width = canvas.TextWidth(font, fontSize, shown) + SpeechPadding * 2;
            int center = anchorOf(speech.Author);
            int left = Math.Clamp(center - width / 2, 1, Math.Max(1, canvas.Width - width - 1));
            Color fill = SpeechFill with { A = alpha };
            Color edge = SpeechEdge with { A = alpha };
            canvas.Fill(left + 1, top, width - 2, height, edge);
            canvas.Fill(left, top + 1, width, height - 2, edge);
            canvas.Fill(left + 1, top + 1, width - 2, height - 2, fill);
            canvas.Fill(Math.Clamp(center, left + 2, left + width - 3), top + height, 1, 1, edge);
            canvas.Text(font, fontSize, shown, left + SpeechPadding, top + 1, edge, fill);
        }
    }

    private static string FitSpeech(PixelCanvas canvas, Font font, int fontSize, string text)
    {
        string shown = text;
        while (shown.Length > 1 && canvas.TextWidth(font, fontSize, shown) > SpeechRoom)
        {
            shown = text[..(shown.TrimEnd('…').Length - 1)].TrimEnd() + "…";
        }

        return shown;
    }

    private static float SpeechSeconds(LaneSpeech speech) => Math.Min(SpeechLongest, SpeechShortest + speech.Text.Length * SpeechPerLetter);

    private static string Fit(PixelCanvas canvas, Font font, string name, int room)
    {
        string shown = name;
        while (shown.Length > 3 && canvas.TextWidth(font, NameFontSize, shown) > room)
        {
            shown = name[..(shown.TrimEnd('.').Length - 1)].TrimEnd() + "..";
        }

        return shown;
    }

    private static Color PopColor(Payment payment) =>
        payment.Perfect ? BarPalette.Gold : payment.Amount > DrinkMenu.Parting ? PlainPop : BarPalette.Muted;
}
