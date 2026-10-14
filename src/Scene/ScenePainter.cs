using IdleBar.Frontier;

namespace IdleBar.Scene;

public sealed class ScenePainter
{
    private ArtRaster _frame = new(1, 1);
    private ArtRaster _layer = new(1, 1);

    public ArtRaster Paint(FrontierState state, FrontierLayout layout, FrontierScene scene, SceneLight light, int hoveredSkill)
    {
        int height = FrontierLayout.Rows + layout.Top;
        if (_frame.Width != layout.Width || _frame.Height != height)
        {
            _frame = new ArtRaster(layout.Width, height);
            _layer = new ArtRaster(layout.Width, height);
        }

        SkyTones sky = SkyTones.For(light);
        int biome = Biomes.IndexOf(state.Stage);
        _frame.Clear();
        _layer.Clear();
        _frame.OffsetY = layout.Top;
        _layer.OffsetY = layout.Top;

        BackdropPainter.Paint(_frame, layout, sky, light, biome, scene.Time);
        GroundPainter.Paint(_layer, layout, biome);
        BuildingPainter.Paint(_layer, state, layout, scene.Motion, scene.Time);
        LandscapePainter.Paint(_layer, layout);
        VillagePainter.Paint(_layer, state, layout, scene);
        FrontPainter.Paint(_layer, state, layout, scene);
        PicoFilter.Apply(_layer);
        PicoFilter.Shade(_layer, sky.NightTint, SkyTones.NightRamp);
        PicoFilter.Shade(_layer, sky.DuskTint, SkyTones.DuskRamp);
        int shake = scene.Motion.Shake > 0 ? ((int)(scene.Time * 40) % 2 == 0 ? 1 : -1) : 0;
        _frame.Over(_layer, shake);

        WeatherPainter.Paint(_frame, layout, light, scene.Time);
        FogPainter.Paint(_frame, state, layout, scene.Motion, sky, scene.Time);
        TitanPainter.Paint(_frame, state, layout, scene);
        TitanPainter.Gauge(_frame, state, layout);
        OverlayPainter.Locks(_frame, state, layout, sky);
        LightPainter.Paint(_frame, state, layout, scene, sky.Lit);
        EffectsPainter.Projectiles(_frame, scene.Fx);
        EffectsPainter.Lightning(_frame, scene.Motion, layout, scene.Time);
        EffectsPainter.Particles(_frame, scene.Fx);
        BossButton.Paint(_frame, state, layout, scene.Time);
        OverlayPainter.Chicken(_frame, state, layout, scene);
        OverlayPainter.Bubble(_frame, layout, scene.Village.Crier);
        OverlayPainter.Floats(_frame, layout, scene.Fx);
        OverlayPainter.Monster(_frame, state, layout, scene);
        TeamPainter.Bars(_frame, state, layout, scene);
        OverlayPainter.Banner(_frame, layout, scene.Banners, scene.Motion, scene.Time);
        SkillPainter.Paint(_frame, state, layout, hoveredSkill, scene.Time);
        EffectsPainter.Coins(_frame, scene.Fx);
        EffectsPainter.Flash(_frame, scene.Motion, layout);
        PicoFilter.Apply(_frame);
        return _frame;
    }
}
