#version 450

layout(set = 0, binding = 2) uniform textureCube CubeTexture;
layout(set = 0, binding = 3) uniform sampler CubeSampler;


#if hasLights
	layout(set = 1, binding = 0) uniform Environment
	{
		vec4 AmbientLight;
	};
#endif

layout(location = 0) in vec3 fsin_position;
layout(location = 0) out vec4 OutputColor;

// TODO: dlete this or move to other file (also licensing!!!!)
//vec4 fog_process(vec3 view, vec3 sky_color) {
//	vec3 fog_color = mix(vec3(0.1, 0.1, 0.1), sky_color, vec3(0.1, 0.1, 0.1));
//
//	if (0.02 > 0.001) {
//		vec4 sun_scatter = vec4(0.0);
//		float sun_total = 0.0;
//		//for (uint i = 0; i < scene_data.directional_light_count; i++) {
//		//	vec3 light_color = directional_lights.data[i].color_size.xyz * directional_lights.data[i].direction_energy.w;
//		//	float light_amount = pow(max(dot(view, directional_lights.data[i].direction_energy.xyz), 0.0), 8.0);
//		//	fog_color += light_color * light_amount * scene_data.fog_sun_scatter;
//		//}
//	}
//
//	float fog_amount = clamp(1.0 - exp(-1000 * 0.5), 0.0, 1.0);
//
//	return vec4(fog_color, fog_amount);
//}

void main()
{
	vec4 textureColor = texture(samplerCube(CubeTexture, CubeSampler), fsin_position);
	//vec4 fog = fog_process(vec3(0, 0, 0), textureColor.rgb);

	//OutputColor = vec4(mix(textureColor.rgb, fog.rgb, fog.a), 1);

	#if hasLights
		textureColor = textureColor * AmbientLight;
	#endif

	OutputColor = textureColor;
}
