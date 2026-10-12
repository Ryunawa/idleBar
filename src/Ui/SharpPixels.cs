using Godot;

namespace IdleBar.Ui;

public static class SharpPixels
{
    private const string Code = """
        shader_type canvas_item;

        varying vec4 tint;

        void vertex() {
            tint = COLOR;
        }

        void fragment() {
            vec2 size = 1.0 / TEXTURE_PIXEL_SIZE;
            vec2 texel = UV * size;
            vec2 scale = max(1.0 / fwidth(texel), vec2(1.0));
            vec2 region = 0.5 - 0.5 / scale;
            vec2 offset = fract(texel) - 0.5;
            vec2 sharp = floor(texel) + (offset - clamp(offset, -region, region)) * scale + 0.5;
            COLOR = texture(TEXTURE, sharp / size) * tint;
        }
        """;

    public static ShaderMaterial Create() => new() { Shader = new Shader { Code = Code } };
}
