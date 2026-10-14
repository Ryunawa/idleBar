using System;
using Godot;
using IdleBar.Frontier;
using IdleBar.Online;
using IdleBar.Scene;

namespace IdleBar.Ui;

public partial class MemberRow : PanelContainer
{
    private const int FrontMax = 3;
    private const double ConvoyFull = 7140;
    private const int SpriteScale = 2;
    private const string ConfirmKick = "Confirmer";

    public event Action<string>? HelpRequested;

    public event Action? KickRequested;

    public static MemberRow Create(MemberData member, bool leader, double heroReady, double convoyReady, double convoyIn)
    {
        MemberRow row = new() { ThemeTypeVariation = WindowSkin.Card };
        HBoxContainer line = new();
        line.AddThemeConstantOverride("separation", 10);
        line.AddChild(new TextureRect
        {
            Texture = SpriteTexture.From(HeroSprites.Of(member.Kind, member.Look).Standing),
            TextureFilter = TextureFilterEnum.Nearest,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(12 * SpriteScale, 17 * SpriteScale),
        });

        VBoxContainer text = new() { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        string crown = member.Leader ? " · chef" : string.Empty;
        string me = member.Me ? " · toi" : string.Empty;
        text.AddChild(new Label { Text = $"{member.Name} ({Roles.Of(member.Kind).Name}){crown}{me}" });
        text.AddChild(WindowRows.Muted(Details(member, convoyIn)));
        line.AddChild(text);

        if (!member.Me)
        {
            line.AddChild(row.Offer("Mon héros", heroReady > 0 || member.Heroes >= FrontMax,
                heroReady > 0 ? $"Ton héros repart dans {GuildMessages.Wait(heroReady)}." : "Ton héros combat 30 min sur son front, en plus du sien.",
                () => row.HelpRequested?.Invoke(SocialApi.HeroHelp)));
            line.AddChild(row.Offer("Convoi", convoyReady > 0 || convoyIn >= ConvoyFull,
                convoyReady > 0 ? $"Ton prochain convoi part dans {GuildMessages.Wait(convoyReady)}." : "+50 % d'or pendant 30 min, jusqu'à 2 h en tout.",
                () => row.HelpRequested?.Invoke(SocialApi.ConvoyHelp)));
            if (leader)
            {
                line.AddChild(row.KickButton());
            }
        }

        row.AddChild(line);
        return row;
    }

    private static string Details(MemberData member, double convoyIn)
    {
        string seen = member.AwaySeconds < 120 ? "en ligne" : $"vu il y a {GuildMessages.Wait(member.AwaySeconds)}";
        string help = member.Heroes > 0 ? $" · {member.Heroes} renfort(s)" : string.Empty;
        string convoy = convoyIn > 0 ? $" · convoi {GuildMessages.Wait(convoyIn)}" : string.Empty;
        return $"Héros niv {member.Level} · record niveau {member.BestStage} · {seen}{help}{convoy}";
    }

    private Button KickButton()
    {
        Button button = new() { Text = "Exclure", TooltipText = "Retire ce joueur de la guilde.", SizeFlagsVertical = SizeFlags.ShrinkCenter };
        button.Pressed += () =>
        {
            if (button.Text == ConfirmKick)
            {
                KickRequested?.Invoke();
                return;
            }

            button.Text = ConfirmKick;
            button.TooltipText = "Clique encore pour l'exclure de la guilde.";
        };
        return button;
    }

    private Button Offer(string text, bool disabled, string tooltip, Action pressed)
    {
        Button button = new() { Text = text, Disabled = disabled, TooltipText = tooltip, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        button.Pressed += pressed;
        return button;
    }
}
