using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public sealed class GuestRoster
{
    private readonly HashSet<long> _served = [];
    private readonly Dictionary<long, DateTimeOffset?> _orderedAt = [];

    public IReadOnlySet<long> Served => _served;

    public void Apply(IReadOnlyList<GuestVisit> guests, IReadOnlyList<Patron> patrons)
    {
        _served.Clear();
        _served.UnionWith(guests.Where(guest => guest.Served).Select(guest => guest.Visit));
        foreach (GuestVisit guest in guests)
        {
            Patron? patron = patrons.FirstOrDefault(each => each.Visit == guest.Visit && each.Phase is not (PatronPhase.Leaving or PatronPhase.Gone));
            bool reordered = _orderedAt.TryGetValue(guest.Visit, out DateTimeOffset? known) && known != guest.OrderedAt;
            _orderedAt[guest.Visit] = guest.OrderedAt;
            if (patron is null)
            {
                continue;
            }

            patron.Dozing = guest.Dozing;
            if (reordered && !guest.Served && patron.Seated && !patron.Incoming)
            {
                patron.OrderAgain(guest.Order);
            }
        }
    }
}
