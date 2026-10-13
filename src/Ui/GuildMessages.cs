using IdleBar.Online;

namespace IdleBar.Ui;

public static class GuildMessages
{
    public static string For(string status, string success, bool help = false) => status switch
    {
        SocialData.Ok => success,
        SocialData.Missing => "Ta partie n'est pas encore sur le serveur.",
        SocialData.NoHero => "Crée d'abord ton héros, dans l'onglet Héros.",
        SocialData.AlreadyMember => "Tu es déjà dans une guilde.",
        SocialData.Cooldown when help => "Cette aide se recharge encore.",
        SocialData.Cooldown => "Tu viens de quitter une guilde : attends un peu avant d'en rejoindre une autre.",
        SocialData.InvalidName => "Le nom doit compter de 3 à 24 lettres, chiffres, espaces, tirets ou apostrophes.",
        SocialData.BannedName => "Ce nom n'est pas accepté. Choisis-en un autre.",
        SocialData.NameTaken => "Une guilde porte déjà ce nom.",
        SocialData.UnknownCode => "Aucune guilde n'a ce code.",
        SocialData.Full => "Cette guilde est complète.",
        SocialData.NotMember => "Tu n'es plus dans cette guilde.",
        SocialData.NotLeader => "Seul le chef de la guilde peut faire ça.",
        SocialData.UnknownMember => "Ce joueur n'est plus dans la guilde.",
        SocialData.Self => "Tu ne peux pas t'envoyer d'aide.",
        SocialData.FrontFull => "Son front accueille déjà trois renforts.",
        SocialData.ConvoyFull => "Ses convois couvrent déjà deux heures.",
        SocialData.Nothing => "Rien à réclamer pour l'instant.",
        _ => "Le serveur ne répond pas. Réessaie dans un instant.",
    };

    public static string Wait(double seconds)
    {
        int minutes = (int)System.Math.Ceiling(seconds / 60);
        return minutes >= 2880 ? $"{minutes / 1440} j" : minutes >= 60 ? $"{minutes / 60} h {minutes % 60:00}" : $"{minutes} min";
    }
}
