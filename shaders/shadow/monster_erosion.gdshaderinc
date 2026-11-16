// Set by the game's hue variation when multiple copies of the same monster spawn.
uniform float h = 0.0;
uniform float s = 1.0;
uniform float v = 1.0;

const int MAX_MARKS = 12;
uniform int mark_count = 0;
uniform vec4 atlas_rects[MAX_MARKS];
uniform vec2 atlas_sizes[MAX_MARKS];
uniform vec4 mark_paths[MAX_MARKS];
uniform vec4 mark_shapes[MAX_MARKS];
varying vec4 tint;

const mat3 RGB_TO_YIQ = mat3(
    vec3(0.2989, 0.5959, 0.2115),
    vec3(0.5870, -0.2774, -0.5229),
    vec3(0.1140, -0.3216, 0.3114));

void vertex() {
    tint = COLOR;
}

vec3 vary_hue(vec3 original) {
    if ((h == 0.0 || h == 1.0) && s == 1.0 && v == 1.0) {
        return original;
    }
    vec3 yiq = RGB_TO_YIQ * original;
    float angle = -h * TAU;
    float cosine = cos(angle);
    float sine = sin(angle);
    yiq.yz = vec2(yiq.y * cosine - yiq.z * sine, yiq.y * sine + yiq.z * cosine) * s;
    return inverse(RGB_TO_YIQ) * (yiq * v);
}

bool inside_region(vec2 uv) {
    return all(greaterThanEqual(uv, vec2(0.0))) && all(lessThanEqual(uv, vec2(1.0)));
}

vec2 unpack_region(vec2 uv, float turns) {
    if (turns == 1.0) return vec2(1.0 - uv.y, uv.x);
    if (turns == 2.0) return 1.0 - uv;
    if (turns == 3.0) return vec2(uv.y, 1.0 - uv.x);
    return uv;
}

vec2 wisp(vec2 p, vec4 path, vec3 shape, float phase) {
    float strand = 0.5 + 0.5 * sin(p.x * 28.0 + p.y * 16.0
        + 1.9 * sin(p.y * 12.0 - phase) + 0.8 * sin(p.x * 23.0 + phase));
    vec2 curl = vec2(sin(p.y * 17.0 - phase), cos(p.x * 21.0 + phase));
    vec2 warped = p + curl * vec2(0.017, 0.027);
    vec2 axis = path.zw - path.xy;
    float along = clamp(dot(warped - path.xy, axis) / dot(axis, axis), 0.0, 1.0);
    float distance = length((warped - path.xy - axis * along) / (shape.xy * 1.14));
    float fog = 1.0 - smoothstep(0.25, 1.12, distance + (strand - 0.5) * 0.38);
    return vec2(fog * shape.z, strand);
}

void fragment() {
    vec4 color = texture(TEXTURE, UV);
    color.rgb = vary_hue(color.rgb);

    vec2 page_size = round(1.0 / TEXTURE_PIXEL_SIZE);
    vec2 pixel = UV * page_size;
    vec2 strongest = vec2(0.0);
    for (int i = 0; i < min(mark_count, MAX_MARKS); i++) {
        if (any(notEqual(page_size, atlas_sizes[i]))) continue;
        vec4 rect = atlas_rects[i];
        vec2 packed_uv = (pixel - rect.xy) / rect.zw;
        if (!inside_region(packed_uv)) continue;
        vec4 shape = mark_shapes[i];
        vec2 mark = wisp(unpack_region(packed_uv, shape.w), mark_paths[i], shape.xyz, TIME * 0.55);
        if (mark.x > strongest.x) strongest = mark;
    }

    float fog = strongest.x;
    float strand = strongest.y;
    float red = smoothstep(0.18, 0.50, fog) * (1.0 - smoothstep(0.60, 0.97, fog)) * strand;
    vec3 ink = mix(vec3(0.016, 0.010, 0.016), vec3(0.30, 0.038, 0.067), red);
    color.rgb = mix(color.rgb, ink, 0.82 * fog * (0.78 + 0.22 * strand));
    COLOR = color * tint;
}
