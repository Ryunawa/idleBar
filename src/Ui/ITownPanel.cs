using System;

namespace IdleBar.Ui;

public interface ITownPanel
{
    event Action<TownCommand>? Requested;

    void Refresh(TownContext context);
}
