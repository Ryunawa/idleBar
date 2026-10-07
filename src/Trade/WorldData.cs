using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleBar.Trade;

public sealed record WorldData(
    IReadOnlyList<GoodInfo> Goods,
    IReadOnlyList<TownInfo> Towns,
    IReadOnlyList<RouteInfo> Routes,
    IReadOnlyList<CraftInfo> Crafts,
    IReadOnlyList<RecipeInfo> Recipes,
    IReadOnlyList<JourneyInfo> Journeys,
    IReadOnlyList<EventKindInfo> EventKinds,
    IReadOnlyList<DirectiveInfo> Directives,
    IReadOnlyList<TalentInfo> Talents,
    IReadOnlyList<FittingInfo> Fittings,
    TradeRules Rules)
{
    public string GoodName(string goodId) => FindGood(goodId)?.Name ?? goodId;

    public string GoodNoun(string goodId, double quantity)
    {
        GoodInfo? good = FindGood(goodId);
        string? noun = quantity < 2 ? good?.OneName : good?.ManyName;
        return noun ?? GoodName(goodId).ToLowerInvariant();
    }

    public GoodInfo? FindGood(string goodId) => Goods.FirstOrDefault(good => good.Id == goodId);

    public bool IsCrafted(string goodId) => FindGood(goodId)?.Crafted == true;

    public TownInfo? FindTown(string townId) => Towns.FirstOrDefault(town => town.Id == townId);

    public string TownName(string townId) => FindTown(townId)?.Name ?? townId;

    public CraftInfo? FindCraft(string craftId) => Crafts.FirstOrDefault(craft => craft.Id == craftId);

    public RecipeInfo? FindRecipe(string recipeId) => Recipes.FirstOrDefault(recipe => recipe.Id == recipeId);

    public IEnumerable<RecipeInfo> RecipesOf(string craftId) => Recipes.Where(recipe => recipe.CraftId == craftId);

    public RouteInfo? FindRoute(string fromTownId, string toTownId) =>
        Routes.FirstOrDefault(route => route.FromTownId == fromTownId && route.ToTownId == toTownId);

    public IEnumerable<RouteInfo> RoutesFrom(string townId) =>
        Routes.Where(route => route.FromTownId == townId).OrderBy(route => route.Seconds);

    public EventKindInfo? FindEventKind(string kindId) => EventKinds.FirstOrDefault(kind => kind.Id == kindId);

    public DirectiveInfo? FindDirective(string directiveId) => Directives.FirstOrDefault(directive => directive.Id == directiveId);

    public JourneyInfo? FindJourney(string fromTownId, string toTownId) =>
        Journeys.FirstOrDefault(journey => journey.FromTownId == fromTownId && journey.ToTownId == toTownId);

    public int FreightFee(string goodId, int quantity, int minutes)
    {
        double value = (FindGood(goodId)?.BasePrice ?? 0) * (double)quantity;
        return Math.Max((int)Math.Ceiling(value * (Rules.FreightBaseRate + Rules.FreightHourlyRate * minutes / 60.0)), 1);
    }
}
