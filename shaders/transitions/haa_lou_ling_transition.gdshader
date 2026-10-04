shader_type canvas_item;
render_mode unshaded;

uniform float threshold : hint_range(0.0, 1.0) = 0.0;

// 三道刀痕分别在 0、0.22、0.44 秒划过；宽度按屏幕高度计算。
vec2 slash_mask(vec2 uv, float start, vec2 from, vec2 to, float bend, vec2 aspect, float edge) {
    float phase = clamp((threshold - start) / 0.22, 0.0, 1.0);
    vec2 delta = (to - from) * aspect;
    float stroke_length = length(delta);
    vec2 direction = delta / stroke_length;
    vec2 offset = (uv - from) * aspect;
    float along = dot(offset, direction) / stroke_length;
    float arc = sin(clamp(along, 0.0, 1.0) * PI);
    float distance_to_blade = abs(dot(offset, vec2(-direction.y, direction.x)) - bend * arc);
    float taper = 0.4 + 0.6 * arc;
    float half_width = mix(0.006, 0.048, smoothstep(0.18, 1.0, phase)) * taper;
    float reach = mix(-0.10, 1.10, smoothstep(0.0, 0.62, phase));
    float sweep = 1.0 - smoothstep(reach - 0.045, reach + 0.015, along);
    float cut = (1.0 - smoothstep(half_width - edge, half_width + edge, distance_to_blade)) * sweep;
    float shine = (1.0 - smoothstep(edge, 0.009, abs(distance_to_blade - half_width))) * sweep;
    shine *= smoothstep(0.0, 0.08, phase) * (1.0 - smoothstep(0.50, 0.90, phase));
    return vec2(cut, shine);
}

void fragment() {
    float edge = max(SCREEN_PIXEL_SIZE.y * 1.5, 0.001);
    vec2 aspect = vec2(SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x, 1.0);
    vec2 first = slash_mask(UV, 0.0, vec2(0.19, -0.03), vec2(1.03, 0.94), -0.022, aspect, edge);
    vec2 second = slash_mask(UV, 0.275, vec2(0.395, -0.04), vec2(0.125, 1.04), 0.018, aspect, edge);
    vec2 third = slash_mask(UV, 0.55, vec2(-0.035, 0.84), vec2(1.035, 0.575), -0.018, aspect, edge);
    float finish = smoothstep(0.84, 1.0, threshold);
    float cut = max(max(first.x, second.x), max(third.x, finish));
    float shine = max(max(first.y, second.y), third.y) * (1.0 - finish);
    COLOR = vec4(vec3(0.78, 1.0, 0.88) * shine, max(cut, shine));
}
