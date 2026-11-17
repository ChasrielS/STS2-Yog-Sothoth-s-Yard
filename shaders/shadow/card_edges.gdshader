shader_type canvas_item;
render_mode unshaded;

uniform sampler2D smoke_noise : filter_linear_mipmap, repeat_enable;
const vec2 frame_size = vec2(300.0, 422.0);
uniform float padding = 18.0;
instance uniform int erosion_rank = 1;
instance uniform float phase_offset = 0.0;

float variation(float seed) {
    return fract(sin(seed * 43.71) * 15278.13);
}

float tendril(vec2 pos, float side, float spacing, vec2 length_range, float thickness, float time, bool extended) {
    float cell = floor(pos.x / spacing);
    float seed = (cell + 13.0) * 28.5 + side * 17.4 + thickness * 4.2;
    float cycle = time * (extended ? 0.125 : 0.36) + variation(seed);
    float age = fract(cycle);
    seed += floor(cycle) * 41.2;
    float fade = extended
        ? smoothstep(0.03, 0.15, age) * (1.0 - smoothstep(0.38, 0.70, age))
        : smoothstep(0.04, 0.28, age) * (1.0 - smoothstep(0.57, 0.98, age));
    float length_px = mix(length_range.x, length_range.y, variation(seed + 1.7)) *
        mix(0.40, 1.0, smoothstep(0.04, extended ? 0.28 : 0.54, age));
    float along = (pos.y + 3.0) / length_px;
    float distance_along = clamp(along, 0.0, 1.0);
    float root = spacing * mix(0.29, 0.71, variation(seed + 3.0));
    float swing = mix(-13.0, 13.0, variation(seed + 4.0));
    float curve = swing * distance_along + sin(distance_along * 7.0 + age * 2.8) * distance_along * 9.2;
    float offset = pos.x - cell * spacing - root - curve;
    float radius = thickness * pow(1.0 - distance_along, 0.85);
    float stem = (1.0 - smoothstep(radius * 0.45, radius + 0.7, abs(offset))) *
        smoothstep(-0.09, 0.06, along) * (1.0 - smoothstep(0.85, 1.0, along));

    float fork_age = clamp((along - 0.26) / 0.46, 0.0, 1.0);
    float fork_offset = offset - sign(swing) * fork_age * (thickness + 4.2);
    float fork = (1.0 - smoothstep(radius * 0.13, radius * 0.44 + 0.6, abs(fork_offset))) *
        smoothstep(0.26, 0.37, along) * (1.0 - smoothstep(0.56, 0.72, along));
    return max(stem, fork * 0.76) * fade;
}

void fragment() {
    vec2 p = (UV - 0.5) * (frame_size + vec2(padding * 2.0));
    float radius = 14.0;
    vec2 corner = abs(p) - frame_size * 0.5 + vec2(radius);
    float edge = length(max(corner, vec2(0.0))) + min(max(corner.x, corner.y), 0.0) - radius;
    vec4 smoke = vec4(0.0);
    float extra = float(erosion_rank - 1);
    float inner_limit = erosion_rank == 3 ? 150.0 : 44.0 + 30.0 * extra;
    if (edge > -inner_limit && edge < padding) {
        vec2 q = p / 335.0;
        vec2 drift = TIME * vec2(0.021, -0.016);
        vec2 warp = vec2(
            texture(smoke_noise, q * 0.78 + drift).r,
            texture(smoke_noise, q * 0.88 + vec2(0.52, 0.29) - drift * 0.72).r
        ) - 0.5;
        float billow = texture(smoke_noise, q * 1.27 + warp * 0.42 + drift * 0.88).r;
        float strands = texture(smoke_noise, q * 2.24 + warp * 0.49 - drift * 0.61).r;
        float depth = edge + (strands - 0.5) * 5.0;
        float outward = 1.0 - smoothstep(2.5, 8.0 + billow * 8.0, depth);
        float rim = outward * smoothstep(-15.0 - 8.0 * extra, -3.0, depth);
        float reach = mix(22.0, 39.0, smoothstep(0.24, 0.73, billow)) + 26.0 * extra;
        float seep = smoothstep(-reach, -5.0, depth) *
            smoothstep(0.28, 0.71, billow * 0.57 + strands * 0.43) * outward;
        float opacity = rim * (0.73 + 0.23 * billow) + (1.0 - rim) * seep * (0.45 + 0.09 * extra);
        float vein = rim * smoothstep(0.48, 0.78, strands) * smoothstep(0.37, 0.75, billow);
        if (erosion_rank > 1) {
            vec2 from_edge = frame_size * 0.5 - abs(p);
            bool vertical = from_edge.x < from_edge.y;
            float side = vertical ? (p.x < 0.0 ? 1.0 : 2.0) : (p.y < 0.0 ? 3.0 : 4.0);
            float width = vertical ? frame_size.y : frame_size.x;
            float along = (vertical ? p.y : p.x) + width * 0.5;
            float inset = min(from_edge.x, from_edge.y);
            float time = TIME + phase_offset;
            vec2 short_reach = erosion_rank == 2 ? vec2(47.0, 64.0) : vec2(48.0, 65.0);
            float growth = tendril(vec2(along, inset), side, erosion_rank == 2 ? 107.0 : 83.0,
                short_reach, 3.0 + extra * 0.60, time, false);
            growth *= smoothstep(12.0, 33.0, min(along, width - along));
            if (erosion_rank == 3) {
                // Each edge has one long tendril with a starting point that changes each cycle.
                const vec2 long_reach = vec2(95.0, 135.0);
                if (from_edge.y < long_reach.y) {
                    vec2 from_horizontal = vec2(p.x + frame_size.x * 0.5, from_edge.y);
                    growth = max(growth, tendril(from_horizontal, p.y < 0.0 ? 3.0 : 4.0,
                        frame_size.x, long_reach, 6.0, time, true));
                }
                if (from_edge.x < long_reach.y) {
                    vec2 from_side = vec2(p.y + frame_size.y * 0.5, from_edge.x);
                    growth = max(growth, tendril(from_side, p.x < 0.0 ? 1.0 : 2.0,
                        frame_size.y, long_reach, 6.0, time, true));
                }
            }
            opacity = max(opacity, growth * (0.71 + strands * 0.25));
            vein = max(vein, growth * (0.31 + strands * 0.41) * smoothstep(0.26, 0.80, billow));
        }
        vec3 ink = mix(vec3(0.019, 0.010, 0.019), vec3(0.32, 0.043, 0.087), vein * 0.76);
        smoke = vec4(ink, opacity);
    }
    COLOR *= smoke;
}
