using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using CameraLib;

using Safari.Components;
using Safari.Services;

namespace Safari
{
	public class Safari : Game
	{
		GraphicsDeviceManager graphics;

		ICamera camera;

		Ground ground;
		Lights lights;
		Tigers tigers;
		Skybox skybox;
		Cubes cubes;
		Line ray;

		Effect effect;

		int width;
		int height;

		KeyboardState prevKeyboardState;
		MouseState prevMouseState;

		bool fogEnabled = true;
		bool shadowMapEnabled = true;
		bool phongShading = true;
		bool wireFrame = false;
		bool ambient = true;
		bool diffuse = true;
		bool specular = true;
		bool directionalLight = false;
		bool spotLight = false;

		CameraTracker cameraTracker;
		CameraHandler cameraHandler;

		float? distance;

		public Safari()
		{
			graphics = new GraphicsDeviceManager(this);
			//graphics.PreferredBackBufferWidth = 1366;
			//graphics.PreferredBackBufferHeight = 768;
			//graphics.IsFullScreen = true;

			Content.RootDirectory = "Content";

			// Komponenten erstellen
			camera = new FreeLookCamera(this);
			ground = new Ground(this);
			lights = new Lights(this);
			skybox = new Skybox(this);
			tigers = new Tigers(this);
			cubes = new Cubes(this);
			ray = new Line(this);

			cameraTracker = new CameraTracker(this, camera);
			cameraHandler = new CameraHandler(this, camera);

			// Komponenten zur Liste hinzufügen
			Components.Add(cameraTracker);
			Components.Add(cameraHandler);
			Components.Add(camera);
			Components.Add(skybox);
			Components.Add(lights);
			Components.Add(tigers);
			Components.Add(ground);
			Components.Add(cubes);
			Components.Add(ray);

			// Services zur Liste hinzufügen
			Services.AddService(typeof(ICameraService), camera);
			Services.AddService(typeof(ILightService), lights);

			// Antialiasing aktivieren
			graphics.PreferMultiSampling = true;
		}


		protected override void Initialize()
		{
			base.Initialize();

			PresentationParameters pp = GraphicsDevice.PresentationParameters;

			width = pp.BackBufferWidth;
			height = pp.BackBufferHeight;

			// Camera Position und View-Vector festlegen
			camera.Position = new Vector3(0.0f, 3.0f, 10.0f);
			camera.LookAt(new Vector3(0.0f, 1.0f, 0.0f));

			float sin = (float)Math.Sin(MathHelper.ToRadians(30.0f));
			float cos = (float)Math.Cos(MathHelper.ToRadians(30.0f));

			float lightHeight = 5.0f;
			float lightRadius = 10.0f;

			// Eigenschaften der Lichter setzen
			lights.NearPlaneDistance = 2.0f;
			lights.FarPlaneDistance = 20.0f;
			lights.Speed = 0.5f;

			lights.Add(0.0f, lightHeight, lightRadius);
			lights.Add(cos * lightRadius, lightHeight, -sin * lightRadius);
			lights.Add(-cos * lightRadius, lightHeight, -sin * lightRadius);

			// Würfel und Pick-Ray auf unsichtbar setzen
			cubes.Visible = false;
			ray.Visible = false;

			// Camera-Tracker deaktivieren
			cameraTracker.Enabled = false;


			// Antialiasing auf 4x setzen
			GraphicsDevice.PresentationParameters.MultiSampleCount = 4;
		}


		protected override void LoadContent()
		{
			// Effect laden
			effect = Content.Load<Effect>("Effects\\Safari");

			// Effect für Tiger und Boden setzen
			tigers.Effect = effect;
			ground.Effect = effect;

			base.LoadContent();
		}


		protected override void Update(GameTime gameTime)
		{
			KeyboardState keyboardState = Keyboard.GetState();
			MouseState mouseState = Mouse.GetState();

			// ESC: Spiel beenden
			if (IsKeyPressed(Keys.Escape))
				Exit();

			// Tabulator: Wireframe ein-/ausschalten
			if (IsKeyPressed(Keys.Tab))
				wireFrame = !wireFrame;
			
			// 1: ShadowMap an/aus
			if (IsKeyPressed(Keys.D1))
				shadowMapEnabled = !shadowMapEnabled;

			// 2: NormalMap an/aus
			if (IsKeyPressed(Keys.D2))
				ground.NormalMapEnabled = !ground.NormalMapEnabled;

			// 3: Phong-Shading an/aus
			if (IsKeyPressed(Keys.D3))
				phongShading = !phongShading;

			// 4: AmbientLight an/aus
			if (IsKeyPressed(Keys.D4))
				ambient = !ambient;

			// 5: DiffuseLight an/aus
			if (IsKeyPressed(Keys.D5))
				diffuse = !diffuse;

			// 6: SpecularLight an/aus
			if (IsKeyPressed(Keys.D6))
				specular = !specular;

			// 7: Nebel an/aus
			if (IsKeyPressed(Keys.D7))
				fogEnabled = !fogEnabled;

			// 8: DirectionalLight an/aus
			if (IsKeyPressed(Keys.D8))
				directionalLight = !directionalLight;

			// 9: Spotlight an/aus
			if (IsKeyPressed(Keys.D9))
				spotLight = !spotLight;

			// 0: CameraTracker an/aus
			if (IsKeyPressed(Keys.D0))
				cameraHandler.Enabled = !(cameraTracker.Enabled = cameraHandler.Enabled);

			// T: Würfel an/aus
			if (IsKeyPressed(Keys.T))
			{
				cubes.Visible = cubes.Enabled = !cubes.Visible;
				ray.Visible = cubes.Visible;
			}

			// Linke Maustaste, wenn Würfel aktiviert
			if (mouseState.LeftButton == ButtonState.Released && prevMouseState.LeftButton == ButtonState.Pressed && cubes.Visible)
			{
				// Distanz zum Schnittpunkt ausgeben
				if (distance != null)
					Console.WriteLine("Hit! Distance: " + distance);
				else
					Console.WriteLine("Missed!");
			}

			base.Update(gameTime);

			// Linke Maustaste
			if (mouseState.LeftButton == ButtonState.Pressed)
			{
				// Intersection Test zwischen Strahl und Würfel
				distance = cubes.FindIntersection(new Ray(camera.Position, camera.Forward));

				// Ende des Strahls auf Schnittpunkt setzen (oder Far-Plane)
				ray.Start = camera.Position;
				ray.End = camera.Position + camera.Forward * ((distance != null) ? (float)distance : camera.FarPlaneDistance);
			}

			prevKeyboardState = keyboardState;
			prevMouseState = mouseState;
		}


		private bool IsKeyPressed(Keys key)
		{
			return prevKeyboardState.IsKeyUp(key) && Keyboard.GetState().IsKeyDown(key);
		}


		protected override void Draw(GameTime gameTime)
		{
			// wenn ShadowMap aktiviert
			if (shadowMapEnabled)
			{
				// Rendern in die ShadowMap im Shader aktivieren
				effect.CurrentTechnique = effect.Techniques["RenderShadowMap"];

				// für alle Lichter
				for (int i = 0; i < lights.Count; i++)
				{
					//GraphicsDevice.SetRenderTarget(null);

					// Rendertarget auf die ShadowMap des aktuellen Lichts setzen
					GraphicsDevice.SetRenderTarget(lights.GetShadowMaps()[i]);

					// Farb- und Tiefenpuffer mit Schwarz bzw. 1.0f initialisieren
					GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

					// ViewProjection Matrix des aktuellen Lichts im Shader setzen
					effect.Parameters["LightViewProjection"].SetValue(lights.GetViewProjections()[i]);

					// Tiger in die ShadowMap zeichnen
					tigers.Draw(gameTime);

					//return;
				}
			}

			// Rendertarget auf Bildschirm setzen
			GraphicsDevice.SetRenderTarget(null);

			// Farb- und Tiefenpuffer mit Schwarz bzw. 1.0f initialisieren
			GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

			// Wireframe aktivieren bzw. deaktivieren
			if (wireFrame && GraphicsDevice.RasterizerState.FillMode == FillMode.Solid)
				GraphicsDevice.RasterizerState = new RasterizerState() { FillMode = FillMode.WireFrame };
			else if (!wireFrame && GraphicsDevice.RasterizerState.FillMode == FillMode.WireFrame)
				GraphicsDevice.RasterizerState = new RasterizerState() { FillMode = FillMode.Solid };

			// Shading im Shader auf Phong oder Gouraud setzen
			effect.CurrentTechnique = (phongShading) ? effect.Techniques["RenderScene"] : effect.Techniques["RenderSceneGouraud"];

			// Shader initialisieren
			effect.Parameters["Lights"].SetValue(lights.GetPositions());
			effect.Parameters["LightViewProjections"].SetValue(lights.GetViewProjections());
			effect.Parameters["Camera"].SetValue(camera.Position);
			effect.Parameters["View"].SetValue(camera.View);
			effect.Parameters["Projection"].SetValue(camera.Projection);
			effect.Parameters["ShadowMapEnabled"].SetValue(shadowMapEnabled);
			effect.Parameters["AmbientEnabled"].SetValue(ambient);
			effect.Parameters["DiffuseEnabled"].SetValue(diffuse);
			effect.Parameters["SpecularEnabled"].SetValue(specular);
			effect.Parameters["DiffusePower"].SetValue(0.5f);
			effect.Parameters["SpecularPower"].SetValue(1.0f);
			effect.Parameters["LightCount"].SetValue(lights.Count);
			effect.Parameters["ShadowStart"].SetValue(lights.NearPlaneDistance);
			effect.Parameters["ShadowEnd"].SetValue(lights.FarPlaneDistance);
			effect.Parameters["FogEnabled"].SetValue(fogEnabled);
			effect.Parameters["FogStart"].SetValue(camera.NearPlaneDistance);
			effect.Parameters["FogEnd"].SetValue(camera.FarPlaneDistance);
			effect.Parameters["FogColor"].SetValue(Vector3.One * 0.5f);
			effect.Parameters["DirectionalLight"].SetValue(directionalLight);
			effect.Parameters["SpotLight"].SetValue(spotLight);
			effect.Parameters["SpotLightCutoff"].SetValue(0.8f);

			for (int i = 0; i < lights.Count; i++)
				effect.Parameters["ShadowMap" + i].SetValue(lights.GetShadowMaps()[i]);

			// Szene auf den Bildschirm zeichnen
			base.Draw(gameTime);
		}
	}
}
