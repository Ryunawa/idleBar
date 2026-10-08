using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public sealed class ServiceDesk
{
    private readonly List<SlidingDrink> _slides = [];

    public IReadOnlyList<SlidingDrink> Slides => _slides;

    public static float HelperDelay(int helper) => helper switch
    {
        >= 3 => 20f,
        2 => 30f,
        1 => 45f,
        _ => float.MaxValue,
    };

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

    public void Help(int helper, IReadOnlyList<Station> stations, IReadOnlyList<Patron> patrons)
    {
        float delay = HelperDelay(helper);
        foreach (Patron patron in patrons.Where(each => each.Visit is null && each.Wants(each.Order) && each.Waited >= delay).ToList())
        {
            int x = stations.FirstOrDefault(station => station.Drink == patron.Order)?.X ?? stations[0].X;
            Send(new PreparedDrink(patron.Order, false, true), patron, x);
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

    private void Send(PreparedDrink drink, Patron patron, int x)
    {
        patron.Incoming = true;
        _slides.Add(new SlidingDrink(drink, patron, x));
    }
}
