shader_type canvas_item;
render_mode unshaded, blend_add;

uniform int stage = 0;
uniform float elapsed = 0.0;
uniform float strength : hint_range(0.0, 1.0) = 0.0;
uniform float anticipation = 0.55;
uniform float duration = 0.82;
uniform bool breaking = false;
varying vec4 sprite_color;

void vertex() {
    sprite_color = COLOR;
}

float hash(float value) {
    return fract(sin(value * 127.1 + 311.7) * 43758.5453);
}

vec4 gather(vec2 uv) {
    vec2 point = (uv - vec2(0.5)) * 2.0;
    float radius = length(point);
    float progress = elapsed / anticipation;
    float fade = smoothstep(0.0, 0.12, progress) * (1.0 - smoothstep(1.0, 1.30, progress));
    float count = 4.0 + floor(strength * 10.0);
    float angle = atan(point.y, point.x) + radius * radius * 0.85 + progress * 0.22;
    float sector = (angle / TAU + 0.5) * count;
    float identity = mod(floor(sector), count);
    float seed = hash(identity);
    float travel = clamp((progress - seed * 0.14) / 0.86, 0.0, 1.0);
    float head = mix(0.90, 0.09, smoothstep(0.0, 1.0, travel));
    float angular_distance = abs(fract(sector) - 0.5) * TAU / count * max(radius, 0.08);
    float streak = smoothstep(head - 0.04, head + 0.01, radius) * (1.0 - smoothstep(head + 0.07, head + 0.26, radius));
    float halo = exp(-angular_distance * angular_distance / 0.0012) * streak * 0.30;
    float core = exp(-angular_distance * angular_distance / 0.000080) * streak * (0.64 + strength * 0.24);
    float tip = exp(-pow((radius - head) / 0.045, 2.0)) * exp(-angular_distance * angular_distance / 0.00018) * 1.02;
    float focus = exp(-radius * radius / 0.050) * smoothstep(0.1, 0.92, progress) * (0.12 + strength * 0.16);
    float ring_distance = abs(radius - mix(0.49, 0.23, travel));
    float arc = exp(-ring_distance * ring_distance / 0.00016) * pow(max(0.0, sin(angle * 3.0 + progress * 2.2)), 6.0) * 0.22;
    float alpha = clamp(max(core + tip, halo) + focus + arc, 0.0, 0.98) * fade;
    vec3 jade = vec3(0.47, 0.89, 0.74);
    vec3 ivory = vec3(1.0, 0.96, 0.79);
    vec3 color = mix(jade, ivory, clamp((core + tip) / max(alpha, 0.001), 0.0, 1.0) * (0.64 + seed * 0.36));
    return vec4(color, alpha);
}

vec4 cut(vec2 uv, sampler2D source_texture) {
    float sweep = smoothstep(0.0, 0.075, elapsed);
    float settle = smoothstep(0.08, 0.28, elapsed);
    float fade_start = (breaking ? 0.33 : 0.24) + strength * 0.12;
    float fade = 1.0 - smoothstep(fade_start, duration, elapsed);
    float width = mix(0.82, 1.0, sweep) * (1.0 - settle * 0.055);
    float thickness = mix(0.42, 0.94 + strength * 0.42, sweep) * (1.0 - settle * 0.14);
    vec2 sample_uv = (uv - vec2(0.5)) / vec2(width, thickness) + vec2(0.5);
    float reach = mix(-0.08, 1.09, sweep);
    float reveal = 1.0 - smoothstep(reach - 0.10, reach + 0.015, uv.x);
    vec4 light = texture(source_texture, clamp(sample_uv, vec2(0.0), vec2(1.0)));
    light.rgb *= mix(vec3(0.83, 0.96, 0.87), vec3(1.10, 1.03, 0.91), strength);
    // 主刀痕保持完整；两侧的薄残光在收势时逐渐碎散。
    float edge = smoothstep(0.02, 0.14, abs(uv.y - 0.51));
    float dissolve = smoothstep(0.34, 0.92, elapsed / duration);
    float grain = hash(floor(uv.x * 91.0) + floor(uv.y * 37.0) * 31.0);
    float peel = 1.0 - edge * dissolve * smoothstep(0.25, 0.9, grain);
    light.a *= reveal * fade * peel * (0.72 + strength * 0.28);
    return light;
}

void fragment() {
    COLOR = (stage == 0 ? gather(UV) : cut(UV, TEXTURE)) * sprite_color;
}
