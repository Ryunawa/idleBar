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

        foreach (GuestData guest in current.Guests.Where(guest => previous.Guests.All(before => before.Visit != guest.Visit)))
        {
            yield return new Announcement($"{guest.Name} pousse la porte de ta taverne !", Seconds);
        }

        if (current.Outing is OutingData outing && previous.Outing?.Visit != outing.Visit)
        {
            yield return new Announcement($"Tu pousses la porte de {outing.Host}…", Seconds);
        }

        if (current.Outing is { ServedAt: not null } served && previous.Outing is { ServedAt: null } waiting && waiting.Visit == served.Visit)
        {
            string how = served.Perfect ? ", servi à la perfection" : served.Helped ? " (son aide était au comptoir)" : string.Empty;
            yield return new Announcement($"{served.Host} t'a servi : {served.Specialty}{how}", Seconds);
        }
    }
}
