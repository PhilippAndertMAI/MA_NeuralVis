shader_type spatial;

uniform vec3 base_color : source_color = vec3(0, 0, 0);
uniform float fresnel_amount = 1.0f;
uniform float effect_strength = 0.9f;

float fresnel(float amount, vec3 normal, vec3 view)
{
	return pow((effect_strength - clamp(dot(normalize(normal), normalize(view)), 0.0, 1.0 )), amount);
}

void fragment()
{
	float basic_fresnel = fresnel(fresnel_amount, NORMAL, VIEW);
	ALBEDO = base_color;
	EMISSION = base_color;
	ALPHA = clamp(basic_fresnel, 0.0, 1.0);
}