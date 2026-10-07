using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace IdleBar.Pixel;

public static class StreetPainter
{
    private const int Margin = 4;
    private const int Gap = 6;
    private const int SpareRoom = 2;
    private const int PileGap = 2;
    private const int CrateStacks = 3;
    private const int CratesPerStack = 2;
    private const int BaseCapacity = 60;
    private const int CapacityStep = 20;
    private const int MaxNotes = 4;
    private const float RingSeconds = 0.4f;
    private const float BlinkSeconds = 0.6f;
    private const int HouseRoom = 8;
    private const int StallGap = 2;
    private const float BlockPhase = 0.37f;

    private static readonly int MarketWidth = TownSprites.For(TownPiece.Stall, false).Width * 2 + StallGap;

    private static readonly Color Note = new("e6dcc3");
    private static readonly Color Pin = new("a8473b");
    private static readonly Color Seal = new("f2c14e");

    public static IReadOnlyList<StreetPlot> Layout(PixelCanvas canvas, StreetState street, CaravanLook? caravan, int products)
    {
        List<StreetPlot> plots = [];
        int x = Margin;
        bool full = false;

        void Add(StreetBuilding building, int width, int height)
        {
            if (full || x + width > canvas.Width - SpareRoom)
            {
                full = true;
                return;
            }

            plots.Add(new StreetPlot(building, x, width, height));
            x += width + Gap;
        }

        if (caravan is CaravanLook look)
        {
            Add(StreetBuilding.Caravan, CaravanPainter.TrainWidth(look.Wagons), CaravanPainter.TrainHeight);
            Add(StreetBuilding.Signpost, StreetSprites.Signpost.Width, StreetSprites.Signpost.Height);
        }
        else
        {
            Add(StreetBuilding.Workshop, WorkshopPainter.Width(street), WorkshopPainter.Height(street));
        }

        Add(StreetBuilding.Warehouse, WarehouseWidth(street, products), Barn(street).Height);
        Add(StreetBuilding.Market, MarketWidth, TownSprites.For(TownPiece.Stall, false).Height);
        Add(StreetBuilding.Counter, StreetSprites.Board.Width, StreetSprites.Board.Height);
        Add(StreetBuilding.Relay, StreetSprites.Relay.Width, StreetSprites.Relay.Height);
        Add(StreetBuilding.Crier, StreetSprites.CrierWaiting.Width, StreetSprites.CrierWaiting.Height);
        return plots;
    }

    public static StreetPlot TravelPlot(PixelCanvas canvas, CaravanLook look) =>
        new(StreetBuilding.Caravan, CaravanPainter.RoadX(canvas), CaravanPainter.TrainWidth(look.Wagons), CaravanPainter.TrainHeight);

    public static void Paint(PixelCanvas canvas, IReadOnlyList<StreetPlot> plots, StreetScene scene, Ambience ambience)
    {
        int ground = LandscapePainter.GroundTop(canvas);
        int start = plots.Count == 0 ? Margin : plots[^1].Right + HouseRoom;
        for (int block = 0; start + TownPainter.Width <= canvas.Width; block++, start += TownPainter.Width + HouseRoom)
        {
            TownPainter.PaintHouses(canvas, start, ambience with { Time = ambience.Time + start * BlockPhase }, false, block);
        }

        foreach (StreetPlot plot in plots)
        {
            PaintBuilding(canvas, plot, scene, ambience, ground);
        }

        if (scene.Errands && plots.Count > 1)
        {
            FolkPainter.PaintErrand(canvas, plots[1].X, plots[^1].Right, ambience);
        }
    }

    private static void PaintBuilding(PixelCanvas canvas, StreetPlot plot, StreetScene scene, Ambience ambience, int ground)
    {
        StreetState street = scene.Street;
        switch (plot.Building)
        {
            case StreetBuilding.Workshop:
                WorkshopPainter.PaintStation(canvas, plot.X, street, ambience, scene.Busy, scene.Errands);
                break;
            case StreetBuilding.Caravan:
                CaravanPainter.PaintParked(canvas, plot.X, scene.Look ?? CaravanLook.Plain, ambience);
                break;
            case StreetBuilding.Signpost:
                DrawOnGround(canvas, StreetSprites.Signpost, plot.X, ground);
                break;
            case StreetBuilding.Warehouse:
                PaintWarehouse(canvas, plot.X, ground, street, scene.Products, scene.Pop);
                break;
            case StreetBuilding.Market:
                PixelSprite stall = TownSprites.For(TownPiece.Stall, false);
                DrawOnGround(canvas, stall, plot.X, ground);
                DrawOnGround(canvas, stall, plot.X + stall.Width + StallGap, ground);
                break;
            case StreetBuilding.Counter:
                PaintBoard(canvas, plot.X, ground, street.OpenOffers);
                break;
            case StreetBuilding.Relay:
                DrawOnGround(canvas, StreetSprites.Relay, plot.X, ground);
                if (street.Contracts > 0 && (int)(ambience.Time / BlinkSeconds) % 2 == 0)
                {
                    canvas.Fill(plot.X + plot.Width - 2, ground - StreetSprites.Relay.Height + 4, 2, 2, Seal);
                }

                break;
            case StreetBuilding.Crier:
                bool ringing = street.News > 0 && (int)(ambience.Time / RingSeconds) % 2 == 0;
                DrawOnGround(canvas, ringing ? StreetSprites.CrierRinging : StreetSprites.CrierWaiting, plot.X, ground);
                break;
        }
    }

    private static void PaintWarehouse(PixelCanvas canvas, int x, int ground, StreetState street, IReadOnlyList<string> products, ProductPop pop)
    {
        PixelSprite barn = Barn(street);
        DrawOnGround(canvas, barn, x, ground);
        int pileX = x + barn.Width + PileGap;
        WorkshopPainter.PaintProducts(canvas, pileX, ground, products, pop);
        int cratesX = pileX + (products.Count > 0 ? WorkshopPainter.ProductsWidth(products.Count) + PileGap : 0);
        int crates = Crates(street);
        for (int index = 0; index < crates; index++)
        {
            int stack = index / CratesPerStack;
            int level = index % CratesPerStack;
            canvas.Draw(StreetSprites.Crate, cratesX + stack * (StreetSprites.Crate.Width + 1), ground - (level + 1) * StreetSprites.Crate.Height + 1);
        }
    }

    private static void PaintBoard(PixelCanvas canvas, int x, int ground, int offers)
    {
        PixelSprite board = StreetSprites.Board;
        int top = ground - board.Height + 1;
        canvas.Draw(board, x, top);
        for (int note = 0; note < Math.Min(offers, MaxNotes); note++)
        {
            int noteX = x + 2 + note % 2 * 4;
            int noteY = top + 1 + note / 2 * 2;
            canvas.Fill(noteX, noteY, 3, 2, Note);
            canvas.Fill(noteX + 1, noteY, 1, 1, Pin);
        }
    }

    private static int WarehouseWidth(StreetState street, int products)
    {
        int width = Barn(street).Width;
        if (products > 0)
        {
            width += PileGap + WorkshopPainter.ProductsWidth(products);
        }

        int crates = Crates(street);
        if (crates > 0)
        {
            width += PileGap + (crates + CratesPerStack - 1) / CratesPerStack * (StreetSprites.Crate.Width + 1);
        }

        return width;
    }

    private static PixelSprite Barn(StreetState street) => StreetSprites.Barn((street.StorageCapacity - BaseCapacity) / CapacityStep);

    private static int Crates(StreetState street) =>
        street.StorageCapacity <= 0 ? 0 : (int)Math.Round(Math.Clamp((double)street.StorageLoad / street.StorageCapacity, 0, 1) * CrateStacks * CratesPerStack);

    private static void DrawOnGround(PixelCanvas canvas, PixelSprite sprite, int x, int ground) => canvas.Draw(sprite, x, ground - sprite.Height + 1);
}