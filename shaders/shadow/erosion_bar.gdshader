shader_type canvas_item;
render_mode unshaded;

uniform float fill_fraction : hint_range(0.0, 1.0) = 0.0;
uniform vec2 bar_size = vec2(250.0, 16.0);
uniform sampler2D mist_noise : filter_linear_mipmap, repeat_enable;
uniform vec4 mist_deep : source_color = vec4(0.52, 0.52, 0.52, 1.0);
uniform vec4 mist_light : source_color = vec4(0.90, 0.90, 0.90, 1.0);
varying vec2 bar_pixel;

void vertex() {
    bar_pixel = VERTEX;
}

vec3 crystal_mist(float x, float y) {
    vec2 stream = vec2((x - TIME * 14.0) / 91.0, y * 0.62);
    float bend = texture(mist_noise, stream * 0.64 + vec2(0.31, 0.49)).r - 0.5;
    float cloud = texture(mist_noise, stream + vec2(bend * 0.42, bend * 0.54)).r;
    vec2 vapor = vec2((x - TIME * 22.0) / 57.0, y * 0.88) + vec2(0.4, 0.53 - bend * 0.21);
    float wisp = texture(mist_noise, vapor + vec2(cloud * 0.17, 0.0)).r;
    float density = smoothstep(0.31, 0.72, cloud * 0.72 + wisp * 0.28);
    vec3 tint = mix(mist_light.rgb, mist_deep.rgb, smoothstep(0.31, 0.72, cloud) * 0.94);
    vec3 core = mix(vec3(0.985), vec3(0.86), y);
    return mix(core, tint, 0.13 + density * 0.84);
}

void fragment() {
    float height = max(bar_size.y, 1.0);
    float x = bar_pixel.x;
    float y = bar_pixel.y / height;
    float slant = abs(y * 2.0 - 1.0);
    float end = min(x, bar_size.x - x) - slant * height * 0.43;
    float face = smoothstep(1.4, 5.2, end) * smoothstep(0.12, 0.24, y) * (1.0 - smoothstep(0.72, 0.86, y));
    float rim = smoothstep(0.0, 1.25, min(min(bar_pixel.y, height - bar_pixel.y), end));
    vec3 upper_facet = mix(vec3(0.94), mist_light.rgb, 0.24);
    vec3 lower_facet = mix(vec3(0.44), mist_deep.rgb, 0.32);
    vec3 facets = mix(upper_facet, lower_facet, smoothstep(0.16, 0.91, y));
    vec3 crystal = mix(facets, crystal_mist(x, y), face);
    float glint = 1.0 - smoothstep(0.30, 1.25, abs(bar_pixel.y - height * 0.14));
    float angled = x / max(bar_size.x, 1.0) + (y - 0.5) * 0.055;
    float refraction = smoothstep(0.22, 0.25, angled) * (1.0 - smoothstep(0.27, 0.38, angled));
    crystal = mix(crystal, vec3(1.0), glint * 0.57 + face * refraction * 0.24);
    crystal = mix(vec3(0.25), crystal, rim);

    float width = bar_size.x * fill_fraction;
    float cap = min(height * 0.5, width * 0.78);
    float tip = width - cap * slant;
    float aa = max(fwidth(tip - x) * 0.5, 0.22);
    float filled = fill_fraction >= 1.0 ? 1.0 : smoothstep(-aa, aa, tip - x) * step(0.0001, width);
    vec3 obsidian = mix(vec3(0.105, 0.087, 0.102), vec3(0.036, 0.028, 0.037), face);
    obsidian = mix(vec3(0.034, 0.028, 0.034), obsidian, rim);
    COLOR.rgb = mix(crystal, obsidian, filled);
}
