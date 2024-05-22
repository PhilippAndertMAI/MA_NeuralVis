// heat map shading adapted from: https://github.com/ericalbers/UnityHeatmapShader/tree/main

shader_type fog;

#include "./ColorUtil.gdshaderinc" 

uniform int n_neurons;
uniform float fog_width;
uniform float fog_height;
uniform float min_pos_x;
uniform float min_pos_y;
uniform float thickness;
uniform vec3[100] positions;
uniform float biases[100];
uniform float avg_weights[100];
uniform float avg_activations[100];

uniform float density : hint_range(1.0, 5.0) = 5.0;
uniform float strength : hint_range(1.0, 5.0) = 5.0;
uniform float size : hint_range(1.0, 2.0) = 1.5;

uniform float[5] _colors;
uniform float[5] _ranges;

struct Globals {
	float base_radius;
	
	int steps;
	vec4[6] colors;
	float[6] ranges;
};

Globals initialize() {
	Globals globals;
	// base radius equals half the distance from one neuron to the next (non-diagonal)
	globals.base_radius = abs(positions[1].x - positions[0].x) * 0.5f;
	globals.steps = 6;
	globals.colors = {
		vec4(0.0, 0.0, 0.0, 0.0),
		vec4(0.0, 0.9, 0.2, 1.0),
		vec4(0.9, 1.0, 0.3, 1.0),
		vec4(0.9, 0.7, 0.1, 1.0),
		vec4(1.0, 0.0, 0.0, 1.0),
		vec4(1.0, 1.0, 1.0, 1.0)
	};
	globals.ranges = { 0.0, 0.167, 0.334, 0.5, 0.668, 1.0 };
	
	return globals;
}

float dist_squared(vec2 uv, vec2 pos, float radius) {
	return pow(max(0.0f, 1.0f - distance(uv, pos) / radius), 2.0f);
}

vec4 get_heat_for_pixel(float weight, Globals globals) {
	if (weight <= globals.ranges[0]) {
		return globals.colors[0];
	}
	if (weight >= globals.ranges[globals.steps - 1]) {
		return globals.colors[globals.steps - 1];
	}
	for (int i = 1; i < globals.steps; i++) {
		if (weight < globals.ranges[i]) {
			float dist_from_lower_point = weight - globals.ranges[i - 1];
            float size_of_point_range = globals.base_radius;

            float ratio_over_lower_point = dist_from_lower_point / size_of_point_range;

            vec4 color_range = globals.colors[i] - globals.colors[i - 1];

            vec4 color_contribution = color_range * ratio_over_lower_point;

            vec4 new_color = globals.colors[i - 1] + color_contribution;
            return new_color;
		}
	}
	return globals.colors[0];
}

void fog() {
	
	Globals globals = initialize();
	
	float intensity = 0.0f;
	
	for (int neuron_index = 0; neuron_index < n_neurons; neuron_index++) {
		// align position by offsetting by min positions (including the thickness of the fog)
		vec3 pos = positions[neuron_index] - vec3(min_pos_x, min_pos_y, 0) + vec3(thickness * 0.5f, thickness * 0.5f, 0.0f);
		
		// scale UV coords to the width of the fog volume
		float u = UVW.x * fog_width;
		float v = UVW.y * fog_height;
		vec2 uv = vec2(u, v);
		
		float bias = biases[neuron_index];
		float avg_weight = avg_weights[neuron_index];
		float avg_activation = avg_activations[neuron_index];
		
		float radius = globals.base_radius * avg_weight + bias;
		radius *= size;
		
		// boost activation value a bit since it ranges around 0
		avg_activation += 1.0f;
		float value = pow(avg_activation, 3);
		value *= strength;
		
		intensity += 0.5f * dist_squared(uv, pos.xy, radius) * value;
	}
	
	vec4 color = get_heat_for_pixel(intensity, globals);
	
	// convert to hsv space, clamp value (brightness) and convert back to rgb
	vec3 hsvColor = rgbToHsv(color.rgb);
	hsvColor.z = clamp(hsvColor.z, 0.0f, 5.0f);
	vec3 emission = hsvToRgb(hsvColor);
	
	ALBEDO = color.rgb;
	DENSITY = color.a * density;
	EMISSION = emission;
}
