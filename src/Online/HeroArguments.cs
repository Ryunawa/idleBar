using System;
using IdleBar.Frontier;

namespace IdleBar.Online;

public sealed record HeroArguments(Guid PSession, string PName, string PRole, int PSkin, int PHair, int PHairColor, int POutfit)
{
    public static HeroArguments From(Guid session, HeroLook look, HeroRole role) =>
        new(session, look.Name, Roles.Of(role).Id, look.Skin, look.Hair, look.HairColor, look.Outfit);
}
