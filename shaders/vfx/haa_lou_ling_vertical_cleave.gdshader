shader_type canvas_item;
render_mode unshaded;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
varying vec4 sprite_color;

void vertex() {
    sprite_color = COLOR;
}

void fragment() {
    float along = clamp((UV.y - 0.07) / 0.86, 0.0, 1.0);
    float taper = pow(max(0.0, sin(along * PI)), 0.72);
    float center = 0.43 + along * 0.13 + sin(along * PI) * 0.035;
    float blade_distance = abs(UV.x - center);
    float sweep = smoothstep(0.0, 0.24, progress);
    float head = mix(-0.08, 1.10, sweep);
    float reveal = 1.0 - smoothstep(head - 0.10, head + 0.015, UV.y);
    float fade = 1.0 - smoothstep(0.44, 1.0, progress);

    // 厚实的单道刀锋由上至下展开，两端收尖，保留普通斩击的白金与青绿残光。
    float width = (0.009 + taper * 0.043) * mix(0.70, 1.0, sweep);
    float core = (1.0 - smoothstep(width * 0.48, width, blade_distance)) * taper;
    float edge = (1.0 - smoothstep(width, width * 2.4, blade_distance)) * taper * 0.48;
    float glow = exp(-blade_distance * blade_distance / 0.0196) * taper * 0.18;
    float wake_distance = UV.x - (center - taper * 0.075);
    float wake = exp(-wake_distance * wake_distance / 0.005625) * taper * 0.18;

    // 命中后，两侧的细残光快速分开，衬托中间纵斩的劈开感。
    float split = smoothstep(0.10, 0.54, progress);
    float split_shape = smoothstep(0.18, 0.36, UV.y) * (1.0 - smoothstep(0.65, 0.92, UV.y));
    float split_distance = abs(blade_distance - (0.040 + split * 0.13) * split_shape);
    float split_light = exp(-split_distance * split_distance / 0.000169) * split_shape * split * 0.36;
    split_light *= 1.0 - smoothstep(0.38, 0.94, progress);

    float alpha = clamp(core + edge + glow + wake + split_light, 0.0, 1.0) * reveal * fade;
    vec3 jade = vec3(0.56, 0.94, 0.78);
    vec3 ivory = vec3(1.0, 0.98, 0.87);
    vec3 color = mix(jade, ivory, clamp(core / max(core + edge + glow + wake, 0.001), 0.0, 1.0));
    COLOR = vec4(color, alpha) * sprite_color;
}
