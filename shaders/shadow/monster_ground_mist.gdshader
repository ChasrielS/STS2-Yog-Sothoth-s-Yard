shader_type canvas_item;
render_mode unshaded;

uniform sampler2D smoke_noise : filter_linear_mipmap, repeat_enable;
instance uniform float strength = 0.70;
instance uniform float phase_offset = 0.0;

void fragment() {
    vec2 p = UV * 2.0 - 1.0;
    float t = TIME + phase_offset;
    vec2 flow = vec2(t * 0.043, -t * 0.027);
    float curl = texture(smoke_noise, UV * vec2(1.24, 0.73) - flow).r;
    float folds = texture(smoke_noise, UV * vec2(2.13, 0.82) + flow * 0.71 + curl * 0.24).r;
    vec2 shape = vec2(p.x * 1.05, (p.y - 0.10 + (curl - 0.5) * 0.48) * 1.50);
    float bank = 1.0 - smoothstep(0.16, 0.99, length(shape));
    float wisps = smoothstep(0.25, 0.73, curl * 0.39 + folds * 0.61);
    float fade = smoothstep(0.0, 0.19, UV.y) * (1.0 - smoothstep(0.72, 0.99, UV.y));
    float red = smoothstep(0.39, 0.74, folds) * smoothstep(0.32, 0.64, curl);
    vec3 ink = mix(vec3(0.020, 0.010, 0.018), vec3(0.36, 0.057, 0.10), red);
    COLOR *= vec4(ink, bank * fade * strength * mix(0.43, 1.0, wisps));
}
