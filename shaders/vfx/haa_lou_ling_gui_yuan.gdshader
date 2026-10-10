shader_type canvas_item;
render_mode unshaded, blend_add;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform float phase = 0.0;
varying vec4 sprite_color;

void vertex() {
    sprite_color = COLOR;
}

void fragment() {
    vec2 uv = UV;
    // 保持灵魂上部稳定，只让拖尾随上浮轻轻摆动。
    float tail = smoothstep(0.37, 0.80, uv.y);
    uv.x -= sin(phase + progress * 8.0) * 0.09375 * tail;
    COLOR = texture(TEXTURE, clamp(uv, vec2(0.0), vec2(1.0))) * sprite_color;
}
