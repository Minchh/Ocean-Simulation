shader_type sky;

uniform vec3 u_sky_color : source_color;
uniform vec3 u_horizon_color : source_color;
uniform sampler2D u_sky_texture;

void sky()
{
	float col = clamp(EYEDIR.y / 0.05, 0.0, 1.0);
	vec3 final_color = mix(u_horizon_color, u_sky_color, col);
	COLOR = mix(texture(u_sky_texture, SKY_COORDS).rgb, final_color, 0.3);
}
