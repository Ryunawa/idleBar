using System.Collections.Generic;
using System.Linq;
using IdleBar.Online;

namespace IdleBar.Ui;

public static class FriendBanners
{
    private const float Seconds = 6;

    public static IEnumerable<Announcement> Describe(TavernData previous, TavernData current)
    {
        foreach (InvitationData invitation in current.Invitations.Where(invitation => previous.Invitations.All(before => before.Id != invitation.Id)))
        {
            yield return new Announcement($"{invitation.Name} t'invite à boire un verre : réponds dans l'onglet Amis de la taverne", Seconds);
        }

        foreach (RequestData request in current.Requests.Where(request => previous.Requests.All(before => before.Id != request.Id)))
        {
            yield return new Announcement($"{request.Name} veut devenir ton ami : réponds dans l'onglet Amis de la taverne", Seconds);
        }

        foreach (FriendData friend in current.Friends.Where(friend => previous.Friends.All(before => before.Id != friend.Id)))
        {
            yield return new Announcement($"{friend.Name} et toi êtes amis !", Seconds);
        }

        HashSet<long> known = Arrivals(previous).Select(guest => guest.Visit).ToHashSet();
        bool sameRoom = previous.Room?.Mine == current.Room?.Mine;
        foreach ((long _, string name) in Arrivals(current).Where(guest => sameRoom && !known.Contains(guest.Visit)))
        {
            yield return new Announcement($"{name} pousse la porte de ta taverne !", Seconds);
        }

        if (current.Outing is OutingData outing && previous.Outing?.Visit != outing.Visit)
        {
            yield return new Announcement($"Tu pousses la porte de {outing.Host}…", Seconds);
        }

        if (current.Outing is { ServedAt: not null, Waiting: false } served
            && previous.Outing is { } before && before.Visit == served.Visit && (before.ServedAt is null || before.Waiting))
        {
            string how = served.Perfect ? " à la perfection" : served.Helped ? " (son aide était au comptoir)" : string.Empty;
            yield return new Announcement($"{served.Host} t'a servi{how} !", Seconds);
        }
    }

    private static IEnumerable<(long Visit, string Name)> Arrivals(TavernData tavern)
    {
        IEnumerable<(long, string)> present = tavern.Room is { Mine: true } room ? room.Guests.Select(guest => (guest.Visit, guest.Name)) : [];
        return tavern.Guests.Select(guest => (guest.Visit, guest.Name)).Concat(present).DistinctBy(guest => guest.Item1);
    }
}
