using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

using CameraLib;
using Safari.Services;

namespace Safari.Components
{
	public class Lights : DrawableGameComponent, ILightService
	{
		public float Speed { get; set; }
		public int Count { get; private set; }

		public float NearPlaneDistance { get; set; }
		public float FarPlaneDistance { get; set; }

		Model model;
		Texture2D texture;
		bool pause;

		KeyboardState prevKeyboardState;

		List<Light> lights;


		// Struktur zum Speicher eines Lichts inkl. ViewProjection Matrix und ShadowMap
		struct Light
		{
			public Light(Vector3 position, GraphicsDevice graphicsDevice)
			{
				Position = position;
				ViewProjection = Matrix.Identity;
				ShadowMap = new RenderTarget2D(graphicsDevice, 2048, 2048, false, SurfaceFormat.Single, DepthFormat.Depth24);
			}

			public Vector3 Position;
			public Matrix ViewProjection;
			public RenderTarget2D ShadowMap;
		}

		public Lights(Game game) : base(game)
		{
			lights = new List<Light>();
		}

		public override void Initialize()
		{
			base.Initialize();
		}

		protected override void LoadContent()
		{
			// Model und Textur laden
			model = Game.Content.Load<Model>("Models\\Sphere");
			texture = Game.Content.Load<Texture2D>("Textures\\Sun");

			base.LoadContent();
		}

		public override void Update(GameTime gameTime)
		{
			float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

			// Space: Lichter anhalten
			if (prevKeyboardState.IsKeyDown(Keys.Space) && Keyboard.GetState().IsKeyUp(Keys.Space))
				pause = !pause;

			prevKeyboardState = Keyboard.GetState();

			Matrix projection;

			// Projektionsmatrix erzeugen
			projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver2, 1.0f, NearPlaneDistance, FarPlaneDistance);

			// für alle Lichter
			for (int i = 0; i < lights.Count; i++)
			{
				Light light = lights[i];

				// wenn keine Pause, Licht um den Mittelpunkt rotieren
				if (!pause)
					light.Position = Vector3.Transform(light.Position, Matrix.CreateRotationY(elapsed * Speed));

				// ViewProjection Matrix des Lichts aktualisieren (für ShadowMapping)
				light.ViewProjection = Matrix.CreateLookAt(light.Position, Vector3.Zero, Vector3.Up) * projection;

				lights[i] = light;
			}

			base.Update(gameTime);
		}

		public override void Draw(GameTime gameTime)
		{
			float elapsed = (float)gameTime.TotalGameTime.TotalSeconds;

			var camera = (ICameraService)Game.Services.GetService(typeof(ICameraService));

			// für alle Lichter
			foreach (Light light in lights)
			{
				// für alle ModelMeshes
				foreach (ModelMesh mesh in model.Meshes)
				{
					// für alle Effecte des Meshes
					foreach (BasicEffect effect in mesh.Effects)
					{
						// Effect Parameter setzen
						effect.World = Matrix.CreateScale(1.0f) * Matrix.CreateRotationY(-elapsed) * Matrix.CreateTranslation(light.Position);
						effect.View = camera.View;
						effect.Projection = camera.Projection;
						effect.Texture = texture;
						effect.TextureEnabled = true;
					}

					// Mesh rendern
					mesh.Draw();
				}
			}

			base.Draw(gameTime);
		}

		// fügt ein Licht zur Liste hinzu
		public void Add(float x, float y, float z)
		{
			lights.Add(new Light(new Vector3(x, y, z), GraphicsDevice));
			Count++;
		}


		// liefert die Positionen der Lichter
		public Vector3[] GetPositions()
		{
			return lights.Select(l => l.Position).ToArray();
		}


		// liefert die ViewProjection Matrices der Lichter
		public Matrix[] GetViewProjections()
		{
			return lights.Select(l => l.ViewProjection).ToArray();
		}

		
		// liefert die ShadowMaps der Lichter
		public RenderTarget2D[] GetShadowMaps()
		{
			return lights.Select(l => l.ShadowMap).ToArray();
		}
	}
}
