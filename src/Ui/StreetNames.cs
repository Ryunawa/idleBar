using IdleBar.Pixel;

namespace IdleBar.Ui;

public static class StreetNames
{
    private const string Merchant = "negociant";

    public static string Of(StreetBuilding building, LaneScene scene) => building switch
    {
        StreetBuilding.Workshop => scene.CraftId == Merchant ? "Ton comptoir" : "Ton atelier",
        StreetBuilding.Warehouse => "Entrepôt",
        StreetBuilding.Market => "Marché",
        StreetBuilding.Counter => "Offres du comptoir",
        StreetBuilding.Relay => "Relais des contrats",
        StreetBuilding.Crier => "Crieur · journal",
        StreetBuilding.Caravan => "Ta caravane",
        _ => "Routes",
    };

    public static TownTab TabOf(StreetBuilding building, LaneScene scene) => building switch
    {
        StreetBuilding.Workshop => scene.CraftId == Merchant ? TownTab.Counter : TownTab.Workshop,
        StreetBuilding.Warehouse => TownTab.Warehouse,
        StreetBuilding.Market => TownTab.Market,
        StreetBuilding.Counter => TownTab.Counter,
        StreetBuilding.Relay => TownTab.Contracts,
        StreetBuilding.Crier => TownTab.Journal,
        StreetBuilding.Caravan => scene.Mode is LaneMode.Travelling or LaneMode.Halted ? TownTab.Journal : TownTab.Caravan,
        _ => TownTab.Routes,
    };
}