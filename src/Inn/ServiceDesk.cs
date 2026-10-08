using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public sealed class ServiceDesk
{
    private const float HostDelay = 3f;
    private const int HostSpread = 7;

    private readonly List<SlidingDrink> _slides = [];

    public IReadOnlyList<SlidingDrink> Slides => _slides;

    public void Dispatch(IReadOnlyList<Station> stations, IReadOnlyList<Patron> patrons)
    {
        foreach (Station station in stations)
        {
            if (station.Ready is null)
            {
                continue;
            }

            Patron? patron = patrons.Where(each => each.Wants(station.Drink)).MaxBy(each => each.Waited);
            if (patron is not null && station.Take() is PreparedDrink drink)
            {
                Send(drink, patron, station.X);
            }
        }
    }

    public void Update(float delta, Action<SlidingDrink> deliver)
    {
        foreach (SlidingDrink slide in _slides)
        {
            slide.Update(delta);
            if (slide.Arrived)
            {
                deliver(slide);
            }
        }

        _slides.RemoveAll(slide => slide.Arrived);
    }

    public void Forget(Patron patron) => _slides.RemoveAll(slide => slide.Patron == patron);

    public void ServeUnattended(IReadOnlyList<Patron> patrons, IReadOnlyList<Station> stations, IReadOnlySet<long> served, bool hosted)
    {
        foreach (Patron patron in patrons.Where(patron => patron.Phase == PatronPhase.Waiting && !patron.Incoming).ToList())
        {
            bool alreadyServed = patron.Visit is long visit && served.Contains(visit);
            bool byHost = hosted && patron.Visit is null && patron.Waited >= HostDelay + patron.Look % HostSpread;
            if (alreadyServed || byHost)
            {
                int x = stations.FirstOrDefault(station => station.Drink == patron.Order)?.X ?? stations[0].X;
                Send(new PreparedDrink(patron.Order, false, alreadyServed), patron, x);
            }
        }
    }

    private void Send(PreparedDrink drink, Patron patron, int x)
    {
        patron.Incoming = true;
        _slides.Add(new SlidingDrink(drink, patron, x));
    }
}
