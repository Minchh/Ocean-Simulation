shader_type spatial;

render_mode diffuse_lambert;

// Water wave parameters
uniform int u_wave_count;
uniform float u_amplitudes[64];
uniform vec2 u_directions[64];
uniform float u_wave_numbers[64];
uniform float u_angular_frequencies[64];

// Water visual parameters
uniform vec3 u_shallow_color;
uniform vec3 u_deep_color;
uniform float u_metallic;
uniform float u_roughness;
uniform float u_specular_strength;

float calculate_total_displacement(vec2 pos)
{
	float result = 0.0;
	
	for (int i = 0; i < u_wave_count; i++)
	{
		vec2 direction = u_directions[i];
		float k = u_wave_numbers[i];
		float freq = u_angular_frequencies[i];
		float amp = u_amplitudes[i];
		
		float phase = dot(direction, pos) * k + freq * TIME;
		
		result += amp * sin(phase);
	}
	
	return result;
}

vec3 calculate_total_normals(vec2 pos)
{
	float ddx = 0.0;
	float ddz = 0.0;
	
	for (int i = 0; i < u_wave_count; i++)
	{
		vec2 direction = u_directions[i];
		float k = u_wave_numbers[i];
		float freq = u_angular_frequencies[i];
		float amp = u_amplitudes[i];
		
		float phase = dot(direction, pos) * k + freq * TIME;
		
		ddx += k * direction.x * amp * cos(phase);
		ddz += k * direction.y * amp * cos(phase);
	}
	
	return normalize(vec3(-ddx, 1.0, -ddz));
}

void vertex()
{
	vec3 world_pos = (MODEL_MATRIX * vec4(VERTEX.xyz, 1.0)).xyz;
	
	VERTEX.y += calculate_total_displacement(world_pos.xz);
	NORMAL = calculate_total_normals(world_pos.xz);
}

void fragment()
{
	float exponent = 5.0 * exp(-2.69 * u_roughness);
	float fresnel = pow(1.0 - max(dot(VIEW, NORMAL), 0.0), exponent);
	
	ALBEDO = mix(u_shallow_color, u_deep_color, fresnel);
	ROUGHNESS = u_roughness;
	METALLIC = u_metallic;
}

void light()
{
	float n_dot_l = max(dot(NORMAL, LIGHT), 0.0);
	
	// Lambertian diffuse
	DIFFUSE_LIGHT += (LIGHT_COLOR / PI) * ATTENUATION * n_dot_l * 0.2;
	
	// Blinn-Phong specular
	vec3 halfway = normalize(VIEW + LIGHT);
	float n_dot_h = max(dot(NORMAL, halfway), 0.0);
	
	// Roughness and shininess convertion
	float shininess = mix(1000.0, 1.0, u_roughness);
	
	// Energy-conserving normalization
	float normalization = (shininess + 8.0) / (8.0 * PI);
	
	SPECULAR_LIGHT += LIGHT_COLOR * ATTENUATION * normalization * pow(n_dot_h, shininess) * u_specular_strength;
}