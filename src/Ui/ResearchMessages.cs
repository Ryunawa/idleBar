using IdleBar.Online;

namespace IdleBar.Ui;

public static class ResearchMessages
{
    public static string For(string status, string? failure, string success) => status switch
    {
        SocialData.Ok => success,
        SocialData.NotMember => "Rejoins une guilde pour verser tes sceaux à sa recherche : tu les gardes en attendant.",
        SocialData.UnknownNode => "Le serveur ne connaît pas cette recherche : mets le jeu à jour.",
        SocialData.Locked => "Cette recherche demande d'abord le nœud précédent de sa branche, terminé au moins au niveau 1.",
        SocialData.Maxed => "Tous les niveaux de cette recherche sont déjà financés.",
        SocialData.TooPoor => "Tu n'as pas assez de sceaux pour ce versement.",
        SocialData.Refused => "Le serveur a refusé ce versement.",
        _ => GuildMessages.Explain(status, failure, success),
    };

    public static string ForMove(string status, string? failure, string success) => status switch
    {
        SocialData.NotLeader => "Seul le chef de la guilde peut changer l'ordre de la file.",
        SocialData.Running => "La recherche en cours garde sa place.",
        SocialData.QueueEdge => "Cette recherche ne peut pas aller plus loin : elle est déjà juste après la recherche en cours, ou à la dernière place.",
        SocialData.NotQueued => "Ce niveau n'est plus en attente : la file a changé entre-temps.",
        SocialData.Refused => "Impossible : deux niveaux d'une même recherche gardent leur ordre.",
        _ => For(status, failure, success),
    };

    public static string Thanks(int given, string name) =>
        given > 0
            ? $"{ResearchSnapshot.Seal(given)} versé{(given > 1 ? "s" : string.Empty)} pour « {name} »."
            : $"« {name} » n'avait plus besoin de sceaux pour ce niveau.";
}
