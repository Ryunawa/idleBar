using IdleBar.Online;

namespace IdleBar.Ui;

public static class HeroMessages
{
    public const string InvalidName = "Le nom doit compter de 3 à 16 lettres, chiffres, espaces, tirets ou apostrophes.";
    public const string Saved = "Héros enregistré.";

    public static string? For(string status) => status switch
    {
        FrontierReply.Ok => null,
        FrontierReply.InvalidName => InvalidName,
        FrontierReply.BannedName => "Ce nom n'est pas accepté. Choisis-en un autre.",
        FrontierReply.RoleLocked => "La classe ne change qu'une fois par repli.",
        FrontierReply.Elsewhere => "Ta partie est ouverte sur un autre PC : reprends-la ici d'abord.",
        _ => "Le serveur ne répond pas. Réessaie dans un instant.",
    };
}
