#define MAX_LIGHTS 3

float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 LightViewProjection;
float4x4 LightViewProjections[MAX_LIGHTS];

float3 Lights[MAX_LIGHTS];
float3 Camera;
float3 FogColor;

float DiffusePower;
float SpecularPower;

float ShadowStart;
float ShadowEnd;

float FogStart;
float FogEnd;

float SpotLightCutoff;

int LightCount;

texture ShadowMap0;
texture ShadowMap1;
texture ShadowMap2;

texture Texture;
texture NormalMap;

bool FogEnabled;
bool TextureEnabled;
bool NormalMapEnabled;
bool ShadowMapEnabled;
bool AmbientEnabled;
bool DiffuseEnabled;
bool SpecularEnabled;
bool DirectionalLight;
bool SpotLight;


sampler2D TextureSampler = sampler_state { 
	Texture = <Texture>; 
	MagFilter = LINEAR; 
	MinFilter = LINEAR; 
	MipFilter = LINEAR;
};


sampler2D NormalMapSampler = sampler_state {
	Texture = <NormalMap>; 
	MagFilter = LINEAR; 
	MinFilter = LINEAR; 
	MipFilter = LINEAR;
};


sampler2D ShadowMapSamplers[MAX_LIGHTS] = {
	sampler_state {
		Texture = <ShadowMap0>; 
		MagFilter = POINT;
		MinFilter = POINT; 
		MipFilter = POINT;
	},
	sampler_state {
		Texture = <ShadowMap1>; 
		MagFilter = POINT;
		MinFilter = POINT; 
		MipFilter = POINT;
	},
	sampler_state {
		Texture = <ShadowMap2>; 
		MagFilter = POINT;
		MinFilter = POINT; 
		MipFilter = POINT;
	}
};


// überprüft, ob die angegebene Position im Schatten liegt
bool IsInShadow(float4 lightPosition, sampler2D shadowMap)
{
	// XY Koordinaten auf der ShadowMap berechnen
	float2 uv = ((lightPosition.xy / lightPosition.w) + float2(1.0f, 1.0f)) / 2.0f;

	uv.y = 1.0f - uv.y;

	// Tiefenwert der Position und der Shadowmap berechnen
	float z = lightPosition.z;
	float pixelDepth = lightPosition.z / lightPosition.w;
	float shadowMapDepth = 1.0f - tex2D(shadowMap, uv).r;

	// wenn Position außerhalb der Shadowmap oder des Schattenbereichs liegt
	if (uv.x < 0.0f || uv.x > 1.0f || uv.y < 0.0f || uv.y > 1.0f || z < ShadowStart || z > ShadowEnd)
		return false;

	float error = 0.001f;

	// Position liegt im Schatten, wenn Tiefenwert der Position größer als Tiefenwert der ShadowMap ist
	return pixelDepth >= shadowMapDepth + error;
}


// liefert 1 wenn WorldPosition im Lichtkegel liegt, sonst 0
float GetCutoff(float3 lightPosition, float3 worldPosition)
{
	// wenn SpotLight deaktiviert oder DirectionalLight aktiviert
	if (!SpotLight || DirectionalLight)
		return 1.0f;

	// Winkel zwischen Vektor von Licht zum Ursprung und von Licht zur WorldPosition ermitteln und mit SpotLightWinkel vergleichen
	return dot(normalize(lightPosition - worldPosition), normalize(lightPosition)) > SpotLightCutoff;
}


// liefert den Lichtvektor von LightPosition zu WorldPosition
float3 GetLightVector(float3 lightPosition, float3 worldPosition)
{
	// wenn DirectionalLight aktiviert -> Lichtvektor konstant
	if (DirectionalLight)
		worldPosition = float3(0.0f, 0.0f, 0.0f);

	return normalize(lightPosition - worldPosition);
}


float3 GetReflection(float3 light, float3 normal)
{
	return reflect(normal, light);
	//return normalize(2.0f * dot(normal, light) * normal - light);
}


// liefert den Wert des ambienten Lichts
float GetAmbient()
{
	if (!AmbientEnabled)
		return 0.0f;

	return 0.2f;
}


// liefert den Wert des diffusen Lichts
float GetDiffuse(float3 light, float3 normal)
{
	if (!DiffuseEnabled)
		return 0.0f;

	return saturate((dot(normal, light))) * DiffusePower;
}


// liefert den Wert des spekulären Lichts
float GetSpecular(float3 view, float3 reflection)
{
	if (!SpecularEnabled)
		return 0.0f;

	return pow(saturate(dot(view, reflection)), 5.0f) * SpecularPower;
}


// liefert die Light Attenuation
float GetAttenuation(float3 light)
{
	if (DirectionalLight)
		return 1.0f;

	return saturate(1.0f - length(light) / 100.0f);
}


// liefert den Farbwert der Textur
float4 GetTextureColor(float2 uv)
{
	if (!TextureEnabled)
		return float4(1.0f, 1.0f, 1.0f, 1.0f);

	return tex2D(TextureSampler, uv);
}


// RENDER SCENE

struct VertexShaderInput
{
	float4 Position		: POSITION0;
	float2 TexCoords		: TEXCOORD0;
	float3 Normal			: NORMAL0;
};


struct VertexShaderOutput
{
	float4 Position				: POSITION0;
	float4 LightPosition			: TEXCOORD1;
	float3 Normal					: TEXCOORD2;
	float3 View						: TEXCOORD3;
	float2 TexCoords				: TEXCOORD4;
	float Depth						: TEXCOORD5;
	float4 WorldPosition			: TEXCOORD6;
};


// VertexShader zum Rendern der Szene
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	float4 position = mul(input.Position, World);

	// Transformationen
	output.Position = mul(position, mul(View, Projection));
	output.LightPosition = mul(position, LightViewProjection);
	output.Depth = output.Position.z;
	output.Normal = normalize(mul(input.Normal, World));
	output.TexCoords = input.TexCoords;
	output.View = normalize(Camera - position);
	output.WorldPosition = position;

	return output;
}


// PixelShader zum Rendern der Szene
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float3 normal = normalize(input.Normal);
	float3 view = normalize(input.View);
	float4 worldPosition = input.WorldPosition;

	// wenn NormalMap aktiviert, Normalvektor aus NormalMap ermitteln
	if (NormalMapEnabled)
		normal = normalize(2.0f * tex2D(NormalMapSampler, input.TexCoords * 2.0f).rbg - 1.0f);

	float diffuse = 0.0f;
	float specular = 0.0f;

	// für alle Lichter
	for (int i = 0; i < LightCount; ++i)
	{
		// LightAttenuation und Cutoff (bei Spotlight) ermitteln
		float modifier = GetAttenuation(Lights[i] - worldPosition) * GetCutoff(Lights[i], worldPosition);

		// Lichtvektor ermitteln
		float3 light = GetLightVector(Lights[i], worldPosition);

		// wenn DirectionalLight aktiviert oder Schatten deaktiviert oder Position nicht im Schatten
		if (DirectionalLight || !ShadowMapEnabled || !IsInShadow(mul(worldPosition, LightViewProjections[i]), ShadowMapSamplers[i]))
		{
			// Diffuse und Specular Light ermitteln
			diffuse += GetDiffuse(light, normal) * modifier;
			specular += GetSpecular(view, GetReflection(light, normal)) * modifier;
		}
	}

	// Farbwert der Texture ermitteln und mit Lichtwerten multiplizieren
	float4 color = GetTextureColor(input.TexCoords) * (saturate(GetAmbient() + diffuse) + specular);

	// wenn Nebel aktiviert
	if (FogEnabled)
	{
		// Distanz zwischen Vertex-Position und Camera berechnen
		float depth = distance(worldPosition, Camera);

		// Stärke des Nebels berechnen
		float fog = saturate((depth - FogStart) / (FogEnd - FogStart));

		// linear zwischen Farbwert und Farbe des Nebels interpolieren
		color = lerp(color, float4(FogColor, 1.0f), fog);
	}

	return color;
}


// RENDER GOURAUD SHADING

struct GouraudVSInput
{
	float4 Position		: POSITION0;
	float3 Normal			: NORMAL0;
	float2 TexCoords		: TEXCOORD0;
};


struct GouraudVSOutput
{
	float4 Position		: POSITION0;
	float2 TexCoords		: TEXCOORD0;
	float	 Light			: TEXCOORD1;
};


// VertexShader zum Rendern der Szene mit Gouraud Shading
GouraudVSOutput GouraudVS(GouraudVSInput input)
{
	GouraudVSOutput output;

	// Transformationen
	float4 position = mul(input.Position, World);
	float3 normal = normalize(mul(input.Normal, World));
	float3 view = normalize(Camera - position);
	float2 texCoords = input.TexCoords;

	float diffuse = 0.0f;
	float specular = 0.0f;
	
	// für alle Lichter
	for (int i = 0; i < LightCount; ++i)
	{
		// Attenuation und Cutoff berechnen
		float attenuation = GetAttenuation(Lights[i] - position);
		float cutoff = GetCutoff(Lights[i], position);

		// Lichtvektor ermitteln
		float3 light = GetLightVector(Lights[i], position);

		// Diffuse und Specular Light berechnen
		diffuse += GetDiffuse(light, normal) * attenuation * cutoff;
		specular += GetSpecular(view, GetReflection(light, normal)) * attenuation * cutoff;
	}

	output.Position = mul(position, mul(View, Projection));
	output.TexCoords = input.TexCoords;
	output.Light = saturate(GetAmbient() + diffuse) + specular;

	return output;
}


// PixelShader zum Rendern der Szene mit Gouraud Shading
float4 GouraudPS(GouraudVSOutput input) : COLOR0
{
	// Farbwert aus der Textur ermitteln und mit interpolierten Lichtwerten multiplizieren
	return GetTextureColor(input.TexCoords) * input.Light;
}


// RENDER SHADOW MAP

struct SMVertexShaderOutput
{
    float4 Position		: POSITION;
    float4 Position2D	: TEXCOORD0;
};


// VertexShader zum Rendern der ShadowMap
SMVertexShaderOutput SMVertexShaderFunction(float4 position : POSITION)
{
    SMVertexShaderOutput output;

    output.Position = mul(position, mul(World, LightViewProjection));

	 // Vertex-Position als Textur-Koordinaten an den PixelShader übergeben
    output.Position2D = output.Position;

    return output;
}


// PixelShader zum Rendern der ShadowMap
float4 SMPixelShaderFunction(SMVertexShaderOutput input) : COLOR0
{       
	// Tiefenwert der Vertex-Position in die ShadowMap schreiben
	return 1.0f - input.Position2D.z / input.Position2D.w;
}


// TECHHNIQUES


technique RenderShadowMap
{
	pass Pass1
	{
		VertexShader = compile vs_2_0 SMVertexShaderFunction();
		PixelShader = compile ps_2_0 SMPixelShaderFunction();
	}
}


technique RenderScene
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 VertexShaderFunction();
		PixelShader = compile ps_3_0 PixelShaderFunction();
	}
}


technique RenderSceneGouraud
{
	pass Pass1
	{
		VertexShader = compile vs_3_0 GouraudVS();
		PixelShader = compile ps_3_0 GouraudPS();
	}
}