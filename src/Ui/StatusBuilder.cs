using IdleBar.Online;

namespace IdleBar.Ui;

public static class StatusBuilder
{
    private const string Title = "La Frontière";

    public static BarStatus Describe(FrontierSession? session)
    {
        if (session is null)
        {
            return Waiting("IdleBar", "Supabase n'est pas configuré");
        }

        return session.Status switch
        {
            SessionStatus.SignedOut => Waiting("Bienvenue !", "Clique ici pour te connecter"),
            SessionStatus.NeedsStart => Waiting(Title, "Clique ici pour prendre la route"),
            SessionStatus.Ready => Waiting(Title, "La route se prépare…"),
            SessionStatus.Offline => Waiting("Hors ligne", "Serveur injoignable"),
            SessionStatus.UpdateRequired => Waiting("Mise à jour requise", "Clique ici pour la télécharger"),
            _ => Waiting("Connexion…", string.Empty),
        };
    }

    private static BarStatus Waiting(string headline, string situation) =>
        new(headline, null, situation, $"{headline} {situation}".Trim());
}
