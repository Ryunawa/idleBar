using IdleBar.Inn;
using IdleBar.Online;

namespace IdleBar.Ui;

public static class StatusBuilder
{
    private const string Title = "IdleBar";

    public static BarStatus Describe(OnlineSession? session, Tavern tavern)
    {
        if (session is null)
        {
            return Waiting(Title, "Supabase n'est pas configuré");
        }

        if (session.Tavern is not TavernData data || !session.Playing)
        {
            return session.Status switch
            {
                SessionStatus.SignedOut => Waiting("Bienvenue !", "Clique ici pour te connecter"),
                SessionStatus.NeedsFounding => Waiting("Ta taverne", "Clique ici pour l'ouvrir"),
                SessionStatus.Offline => Waiting("Hors ligne", "Serveur injoignable"),
                _ => Waiting("Connexion…", string.Empty),
            };
        }

        string situation = Situation(tavern) + (session.Status == SessionStatus.Offline ? " · hors ligne" : string.Empty);
        return new BarStatus(
            data.Name,
            session.Coins,
            situation,
            $"{data.Name} · {NumberFormat.Coins(session.Coins)} · {situation}",
            TipJar(data.TipJar));
    }

    private static BarStatus Waiting(string headline, string situation) =>
        new(headline, null, situation, $"{Title} · {headline} {situation}".Trim(), null);

    private static string Situation(Tavern tavern) => tavern.Waiting switch
    {
        0 => "Salle tranquille",
        1 => "1 client attend",
        int waiting => $"{waiting} clients attendent",
    };

    private static TipJarView? TipJar(TipJarData jar)
    {
        if (jar.Hourly == 0 && jar.Amount == 0)
        {
            return null;
        }

        string tooltip = jar.Amount >= 1
            ? $"Pot à pourboires : {jar.Amount} écus. Clique pour les mettre dans ta bourse."
            : $"Pot à pourboires : ton aide y verse {jar.Hourly} écus par heure pendant ton absence, jusqu'à {jar.Cap}.";
        return new TipJarView(jar.Amount, jar.Cap > 0 && jar.Amount >= jar.Cap, tooltip);
    }
}
