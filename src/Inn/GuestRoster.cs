using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public sealed class GuestRoster
{
    private readonly HashSet<long> _served = [];
    private readonly Dictionary<long, DateTimeOffset?> _orderedAt = [];
    private readonly Dictionary<long, Drink> _changed = [];

    public IReadOnlySet<long> Served => _served;

    public static IReadOnlySet<long> None { get; } = new HashSet<long>();

    public static List<Patron> Departed(IReadOnlyList<GuestVisit> guests, IReadOnlyList<Patron> patrons) =>
        patrons.Where(patron =>
            patron.Visit is long visit
            && patron.Phase is not (PatronPhase.Leaving or PatronPhase.Gone)
            && guests.All(guest => guest.Visit != visit)).ToList();

    public bool Refuse(Patron patron)
    {
        if (patron.Visit is not long visit || !_changed.Remove(visit, out Drink order))
        {
            return false;
        }

        patron.Incoming = false;
        patron.OrderAgain(order);
        return true;
    }

    public void Apply(IReadOnlyList<GuestVisit> guests, IReadOnlyList<Patron> patrons)
    {
        _served.Clear();
        _served.UnionWith(guests.Where(guest => guest.Served).Select(guest => guest.Visit));
        foreach (GuestVisit guest in guests)
        {
            Patron? patron = patrons.FirstOrDefault(each => each.Visit == guest.Visit && each.Phase is not (PatronPhase.Leaving or PatronPhase.Gone));
            if (patron is not null)
            {
                patron.Dozing = guest.Dozing;
            }

            if (guest.OrderedAt is null)
            {
                continue;
            }

            bool reordered = _orderedAt.TryGetValue(guest.Visit, out DateTimeOffset? known) && known != guest.OrderedAt;
            _orderedAt[guest.Visit] = guest.OrderedAt;
            if (patron is null || !reordered || guest.Served)
            {
                continue;
            }

            if (patron.Phase == PatronPhase.Entering)
            {
                patron.ChangeOrder(guest.Order);
                continue;
            }

            if (!patron.Seated)
            {
                continue;
            }

            if (patron.Incoming)
            {
                _changed[guest.Visit] = guest.Order;
                continue;
            }

            patron.OrderAgain(guest.Order);
        }
    }
}
