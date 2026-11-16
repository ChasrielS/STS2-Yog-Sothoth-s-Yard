shader_type canvas_item;
render_mode unshaded;

uniform sampler2D smoke_noise : filter_linear_mipmap, repeat_enable;
uniform bool top_edge = true;

void fragment() {
    vec2 uv = UV;
    float aspect = SCREEN_PIXEL_SIZE.y / SCREEN_PIXEL_SIZE.x;
    float side = min(uv.x, 1.0 - uv.x) * aspect;
    float vertical = top_edge ? min(uv.y * 1.26, 1.0 - uv.y) : 1.0 - uv.y;
    float corner = (1.0 - smoothstep(0.0, 0.18, side)) * (1.0 - smoothstep(0.0, 0.21, vertical));
    float edge = min(side, vertical) * 4.0 - corner * 0.012;
    vec4 smoke = vec4(0.0);
    if (edge < 0.26) {
        vec2 p = vec2(uv.x * aspect, uv.y);
        vec2 drift = TIME * vec2(0.013, -0.008);
        vec2 warp = vec2(
            texture(smoke_noise, p * 0.49 + drift).r,
            texture(smoke_noise, p * 0.53 - drift * 0.61 + vec2(0.36, 0.22)).r
        ) - 0.5;
        float billow = texture(smoke_noise, p * 0.71 + warp * 0.31 - drift * 0.76).r;
        float strands = texture(smoke_noise, p * vec2(1.61, 1.32) + warp * 0.46 + drift * 0.83).r;
        float width = mix(0.046, 0.145, smoothstep(0.27, 0.78, billow)) + corner * 0.025;
        float distance = edge + (strands - 0.5) * 0.028;
        float bank = 1.0 - smoothstep(0.005, width, distance);
        float vapor = (1.0 - smoothstep(width * 0.49, width * 1.45, distance)) *
            smoothstep(0.39, 0.78, billow * 0.51 + strands * 0.49);
        float alpha = bank * (0.43 + 0.39 * billow) + vapor * (1.0 - bank) * 0.35;
        float glint = smoothstep(0.49, 0.74, strands) * smoothstep(0.35, 0.67, billow) *
            (1.0 - smoothstep(0.72, 1.0, bank));
        vec3 ink = mix(vec3(0.020, 0.012, 0.020), vec3(0.32, 0.044, 0.087), glint);
        float fade = top_edge ? 1.0 : smoothstep(0.0, 0.0135, uv.y);
        smoke = vec4(ink, alpha * fade * (1.0 - smoothstep(0.22, 0.26, edge)));
    }
    COLOR *= smoke;
}
