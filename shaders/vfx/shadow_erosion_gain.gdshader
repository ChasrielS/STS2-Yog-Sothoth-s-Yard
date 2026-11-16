shader_type canvas_item;
render_mode unshaded;

uniform sampler2D smoke_noise : repeat_enable, filter_linear;
uniform float progress : hint_range(0.0, 1.0) = 0.0;

float rising_wisp(vec2 p, float x, float delay, float lift, float grain) {
    float age = clamp((progress - delay) / (1.0 - delay), 0.0, 1.0);
    float head = 0.09 + lift * age;
    float tail = max(0.025, head - mix(0.19, 0.37, smoothstep(0.0, 0.65, age)));
    float along = clamp((p.y - tail) / (head - tail), 0.0, 1.0);
    float center = x + sin(p.y * 9.0 - age * 4.0 + x * 8.0) * (0.035 + 0.055 * along);
    float width = mix(0.18, 0.095, along) * mix(0.8, 1.2, grain);
    float filament = 1.0 - smoothstep(width * 0.22, width, abs(p.x - center));
    float ends = smoothstep(tail - 0.02, tail + 0.08, p.y) * (1.0 - smoothstep(head - 0.03, head + 0.09, p.y));
    float fade = smoothstep(0.0, 0.18, age) * (1.0 - smoothstep(0.42, 1.0, age));
    return filament * ends * fade;
}

void fragment() {
    vec2 p = vec2(UV.x * 2.0 - 1.0, 1.0 - UV.y);
    vec2 drift = vec2(0.08, -0.64) * progress;
    float flow = texture(smoke_noise, p * vec2(0.86, 0.92) + drift).r;
    float grain = texture(smoke_noise, p * vec2(1.85, 1.62) + vec2(flow * 0.31, flow * 0.14) + drift).r;
    vec2 warped = p + vec2((flow - 0.5) * 0.15, (grain - 0.5) * 0.09);
    float mist = max(rising_wisp(warped, -0.35, 0.0, 0.65, grain), rising_wisp(warped, 0.34, 0.12, 0.59, grain));
    mist = max(mist, 0.74 * rising_wisp(warped, -0.02, 0.24, 0.71, grain));
    float density = mist * mix(0.33, 1.0, smoothstep(0.22, 0.73, grain));
    float red = smoothstep(0.30, 0.68, flow) * (1.0 - smoothstep(0.44, 0.88, grain));
    vec3 ink = mix(vec3(0.025, 0.013, 0.021), vec3(0.43, 0.058, 0.088), red);
    float border = smoothstep(0.01, 0.09, p.y) * (1.0 - smoothstep(0.83, 1.0, max(abs(p.x), p.y)));
    COLOR = vec4(ink, density * border * 0.79);
}
