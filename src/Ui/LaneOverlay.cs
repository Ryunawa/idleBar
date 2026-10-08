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


    private static readonly Color PlainPop = new("efe6d2");

    private readonly List<LanePop> _pops = [];
    private readonly SpeechLayer _speech = new();
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
        _speech.Advance(delta);
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

    public void Speak(Guid author, string name, string text) => _speech.Speak(author, name, text);

    public void PaintSpeech(PixelCanvas canvas, Font font, int top, Func<Guid, int> anchorOf) => _speech.Paint(canvas, font, top, anchorOf);

    private static string Fit(PixelCanvas canvas, Font font, string name, int room)
    {
        string shown = name;
        while (shown.Length > 3 && canvas.TextWidth(font, NameFontSize, shown) > room)
        {
            int keep = shown.TrimEnd('.').Length - 1;
            if (keep <= 0)
            {
                break;
            }

            shown = name[..keep].TrimEnd() + "..";
        }

        return shown;
    }

    private static Color PopColor(Payment payment) =>
        payment.Perfect ? BarPalette.Gold : payment.Amount > DrinkMenu.Parting ? PlainPop : BarPalette.Muted;
}
