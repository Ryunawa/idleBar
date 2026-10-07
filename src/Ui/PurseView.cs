using System;
using IdleBar.Trade;

namespace IdleBar.Ui;

public sealed record PurseView(int Amount, bool Full, string Tooltip)
{
    public static PurseView? For(OddJobsState? oddJobs, DateTimeOffset now)
    {
        if (oddJobs is null)
        {
            return null;
        }

        int amount = oddJobs.EarnedAt(now);
        bool full = oddJobs.IsFull(now);
        string rate = $"{NumberFormat.Coins(oddJobs.Hourly)} de l'heure";
        string tooltip = (amount, full) switch
        {
            (_, true) => $"Ta bourse est pleine : {NumberFormat.Coins(amount)} de petits services. Clique pour les récupérer, elle se remplit de nouveau ensuite.",
            (< 1, _) => $"Petits services : entre deux tâches, tu rends de menus services en ville pour {rate}. Ta bourse sera pleine à {DurationFormat.ClockTime(oddJobs.FullAt)}.",
            _ => $"Petits services : {NumberFormat.Coins(amount)} en attente ({rate}), bourse pleine à {DurationFormat.ClockTime(oddJobs.FullAt)}. Clique pour les récupérer.",
        };
        return new PurseView(amount, full, tooltip);
    }
}