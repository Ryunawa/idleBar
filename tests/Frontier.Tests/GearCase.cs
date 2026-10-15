using System;
using System.Collections.Generic;
using System.Linq;
using IdleBar.Frontier;

namespace IdleBar.Tests;

public sealed class GearCase
{
    private const double WindowSeconds = 3 * 3600;
    private const int Offered = 4;
    private const int BagSize = 60;

    private readonly List<GearPiece> _pieces = [];
    private readonly List<Gear> _values = [];
    private readonly List<(GearPiece Piece, Gear Value, double Price)> _offers = [];
    private readonly int[][] _worn = [.. Fighters.All.Select(_ => new int[Items.Slots.Count])];
    private readonly Random _random;
    private long _window = -1;

    public GearCase(int seed)
    {
        _random = new Random(seed);
        Unwear();
    }

    public int Count => _pieces.Count;

    public int Offers => _offers.Count;

    public double Price(int offer) => _offers[offer].Price;

    public Gear Worn(int fighter) => Enumerable.Range(0, Items.Slots.Count)
        .Where(slot => _worn[fighter][slot] >= 0).Aggregate(Gear.None, (sum, slot) => GearWorth.Sum(sum, _values[_worn[fighter][slot]]));

    public void Restock(FrontierState state, double clock)
    {
        long window = (long)(clock / WindowSeconds);
        if (window == _window)
        {
            return;
        }

        _window = window;
        _offers.Clear();
        for (int index = 0; index < Offered; index++)
        {
            GearPiece piece = GearPiece.Draw(_random, state, ItemRolls.ShopOdds, true);
            _offers.Add((piece, Gear.Of(piece.Stats()), GearPrices.Offer(state, state.MaxStage, piece.Rarity)));
        }
    }

    public void Loot(FrontierGame game)
    {
        if (_random.NextDouble() * 100 >= ItemRolls.LootChance)
        {
            return;
        }

        Keep(game, GearPiece.Draw(_random, game.State, ItemRolls.BossOdds, false));
        Wear(game);
    }

    public void Wear(FrontierGame game)
    {
        Assign(game.State);
        foreach (FighterInfo fighter in Fighters.All)
        {
            game.Equip(fighter.Kind, Worn((int)fighter.Kind), GearLook.None);
        }
    }

    public (double Cost, double Gain)? Upgrade(GearWorth worth, FrontierState state, int fighter, int slot)
    {
        int piece = _worn[fighter][slot];
        if (piece < 0 || _pieces[piece].Upgrades >= Balance.MaxUpgrades)
        {
            return null;
        }

        Gear[] gears = Current();
        double before = worth.Score(gears);
        gears[fighter] = GearWorth.Sum(GearWorth.Minus(gears[fighter], _values[piece]), Gear.Of(_pieces[piece].Stats(1)));
        return (GearPrices.Upgrade(state, _pieces[piece].Rarity, _pieces[piece].Upgrades, 1), worth.Score(gears) / before - 1);
    }

    public bool Upgrade(FrontierGame game, int fighter, int slot)
    {
        int piece = _worn[fighter][slot];
        double cost = piece < 0 ? double.PositiveInfinity : GearPrices.Upgrade(game.State, _pieces[piece].Rarity, _pieces[piece].Upgrades, 1);
        if (game.State.Gold < cost)
        {
            return false;
        }

        game.State.Gold -= cost;
        _pieces[piece] = _pieces[piece] with { Upgrades = _pieces[piece].Upgrades + 1 };
        _values[piece] = Gear.Of(_pieces[piece].Stats());
        Wear(game);
        return true;
    }

    public double OfferGain(GearWorth worth, FrontierState state, int offer)
    {
        (GearPiece piece, Gear value, double _) = _offers[offer];
        Gear[] gears = Current();
        double before = worth.Score(gears);
        double best = 0;
        int slot = Index(piece.Slot);
        foreach (FighterInfo fighter in Fighters.All.Where(fighter => piece.Fits(state, fighter.Kind)))
        {
            int index = (int)fighter.Kind;
            Gear kept = gears[index];
            gears[index] = GearWorth.Sum(_worn[index][slot] >= 0 ? GearWorth.Minus(kept, _values[_worn[index][slot]]) : kept, value);
            best = Math.Max(best, worth.Score(gears) / before - 1);
            gears[index] = kept;
        }

        return best;
    }

    public bool Buy(FrontierGame game, int offer)
    {
        (GearPiece piece, Gear _, double price) = _offers[offer];
        if (game.State.Gold < price)
        {
            return false;
        }

        game.State.Gold -= price;
        _offers.RemoveAt(offer);
        Keep(game, piece);
        Wear(game);
        return true;
    }

    private static int Index(string slot) => Items.Slots.ToList().IndexOf(slot);

    private Gear[] Current() => [.. Enumerable.Range(0, Fighters.All.Count).Select(Worn)];

    private void Keep(FrontierGame game, GearPiece piece)
    {
        HashSet<int> worn = [.. _worn.SelectMany(row => row).Where(index => index >= 0)];
        _pieces.Add(piece);
        _values.Add(Gear.Of(piece.Stats()));
        if (_pieces.Count - worn.Count <= BagSize)
        {
            return;
        }

        int worst = Enumerable.Range(0, _pieces.Count).Where(index => !worn.Contains(index)).MinBy(index => _pieces[index].Rank);
        game.Recycle(_pieces[worst].Rarity, _pieces[worst].Bought, _pieces[worst].Stage);
        _pieces.RemoveAt(worst);
        _values.RemoveAt(worst);
        Unwear();
    }

    private void Unwear() => Array.ForEach(_worn, row => Array.Fill(row, -1));

    private void Assign(FrontierState state)
    {
        GearWorth worth = new(state);
        Unwear();
        Gear[] gears = [.. Fighters.All.Select(_ => Gear.None)];
        bool[] taken = new bool[_pieces.Count];
        for (int slot = 0; slot < Items.Slots.Count; slot++)
        {
            foreach (FighterInfo fighter in Fighters.All)
            {
                int index = (int)fighter.Kind;
                Gear kept = gears[index];
                double best = worth.Score(gears);
                for (int piece = 0; piece < _pieces.Count; piece++)
                {
                    if (taken[piece] || _pieces[piece].Slot != Items.Slots[slot] || !_pieces[piece].Fits(state, fighter.Kind))
                    {
                        continue;
                    }

                    gears[index] = GearWorth.Sum(kept, _values[piece]);
                    double score = worth.Score(gears);
                    if (score > best)
                    {
                        best = score;
                        _worn[index][slot] = piece;
                    }
                }

                gears[index] = _worn[index][slot] >= 0 ? GearWorth.Sum(kept, _values[_worn[index][slot]]) : kept;
                if (_worn[index][slot] >= 0)
                {
                    taken[_worn[index][slot]] = true;
                }
            }
        }
    }
}
