using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Inn;

public static class Orders
{
    private const double FavoriteChance = 0.7;

    public static Drink Pick(string? regular, IRegularBook? book, IReadOnlyList<Station> stations, Random random)
    {
        if (regular is not null && book?.Favorite(regular) is Drink favorite && stations.Any(station => station.Drink == favorite) && random.NextDouble() < FavoriteChance)
        {
            return favorite;
        }

        int roll = random.Next(stations.Sum(station => DrinkMenu.Appetite(station.Drink)));
        foreach (Station station in stations)
        {
            roll -= DrinkMenu.Appetite(station.Drink);
            if (roll < 0)
            {
                return station.Drink;
            }
        }

        return stations[0].Drink;
    }
}
