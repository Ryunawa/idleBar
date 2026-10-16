using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IdleBar.Frontier;
using IdleBar.Scene;
using Xunit;

namespace IdleBar.Tests;

public sealed class HospitalityTests
{
    [Theory]
    [InlineData(815)]
    [InlineData(538)]
    public void AFourthAllyFindsRoomOnTheFront(int width)
    {
        FrontierState state = SceneTests.SampleState(47);
        state.Hero = new HeroLook("Aldric", 0, 0, 1, 0);
        state.Allies.AddRange(
        [
            new Ally(1, new HeroLook("Brune", 1, 1, 3, 3), HeroRole.Tank, 1, 900),
            new Ally(2, new HeroLook("Corentin", 3, 2, 4, 6), HeroRole.Support, 1, 900),
            new Ally(3, new HeroLook("Dalia", 2, 1, 2, 4), HeroRole.Archer, 1, 900),
            new Ally(4, new HeroLook("Elouan", 0, 2, 5, 2), HeroRole.Mage, 1, 900),
        ]);
        FrontierScene scene = new(new Random(3));
        FrontierLayout layout = FrontierLayout.For(width, FrontierLayout.Rows);
        FrontierGame game = new(state, new Random(3));
        for (int tick = 0; tick < 60; tick++)
        {
            game.Advance(1 / 30.0);
            scene.Update(1 / 30f, state, game.Events.Drain(), layout, false);
        }

        IReadOnlyList<Post> posts = Formation.Posts(state, layout);
        Assert.Equal(7, posts.Count);
        Assert.True(posts.Zip(posts.Skip(1)).All(pair => pair.Second.X > pair.First.X));
        ArtRaster frame = new ScenePainter().Paint(state, layout, scene, SceneLight.Noon, -1);
        Assert.Equal(width, frame.Width);
        if (Environment.GetEnvironmentVariable(SceneTests.SnapshotFolder) is string folder)
        {
            Directory.CreateDirectory(folder);
            PngWriter.Write(frame, Path.Combine(folder, $"hospitalite-{width}.png"), 4);
        }
    }
}
