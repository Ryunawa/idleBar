using System.Collections.Generic;
using IdleBar.Inn;

namespace IdleBar.Online;

public sealed class ServiceLedger
{
    private readonly Dictionary<string, int> _drinks = [];
    private int _perfect;
    private int _parting;

    public long Coins { get; private set; }

    public bool Empty => Coins == 0 && _parting == 0 && _drinks.Count == 0;

    public void Record(Payment payment)
    {
        Coins += payment.Amount;
        if (payment.Drink is not Drink drink)
        {
            _parting++;
            return;
        }

        string id = DrinkMenu.Id(drink);
        _drinks[id] = _drinks.GetValueOrDefault(id) + 1;
        _perfect += payment.Perfect ? 1 : 0;
    }

    public ServiceReport Take()
    {
        ServiceReport report = new(new Dictionary<string, int>(_drinks), _perfect, _parting, Coins);
        _drinks.Clear();
        _perfect = 0;
        _parting = 0;
        Coins = 0;
        return report;
    }

    public void Restore(ServiceReport report)
    {
        foreach ((string id, int count) in report.Drinks)
        {
            _drinks[id] = _drinks.GetValueOrDefault(id) + count;
        }

        _perfect += report.Perfect;
        _parting += report.Parting;
        Coins += report.Coins;
    }

    public void Clear() => Take();
}
