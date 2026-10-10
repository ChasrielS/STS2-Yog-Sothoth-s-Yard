shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
varying vec4 sprite_color;

void vertex() {
    sprite_color = COLOR;
}

void fragment() {
    float open = smoothstep(0.0, 0.28, progress);
    float close = smoothstep(0.48, 1.0, progress);
    vec2 size = vec2(mix(0.84, 1.0, open) * (1.0 - close * 0.32),
                     mix(0.40, 1.0, open) * (1.0 - close * 0.70));
    vec2 uv = (UV - vec2(0.5) - vec2((progress - 0.4) * 0.045, 0.0)) / size + vec2(0.5);
    float reach = mix(-0.04, 1.06, smoothstep(0.0, 0.22, progress));
    float sweep = 1.0 - smoothstep(reach - 0.08, reach + 0.02, UV.x);
    COLOR = texture(TEXTURE, clamp(uv, vec2(0.0), vec2(1.0))) * sprite_color;
    COLOR.a *= sweep * (0.6 + 0.4 * open) * (1.0 - close);
}
