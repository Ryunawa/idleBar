using System;
using System.Collections.Generic;

namespace IdleBar.Inn;

public interface IRegularBook
{
    string? Pick(IReadOnlyCollection<string> present, Random random);

    Drink? Favorite(string regular);
}
