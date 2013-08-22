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
	public class Tigers : DrawableGameComponent
	{
		// Struct zum Speichern der WorldMatrix eines Tigers
		struct Tiger
		{
			public Tiger(Matrix world)
			{
				World = world;
				WorldInverseTranspose = Matrix.Transpose(Matrix.Invert(world));
			}

			public Matrix World;
			public Matrix WorldInverseTranspose;
		}

		public Effect Effect { get; set; }

		List<Tiger> tigers;

		Model model;
		Texture2D texture;

		public Tigers(Game game) : base(game)
		{
			tigers = new List<Tiger>();
		}


		public override void Initialize()
		{
			base.Initialize();

			Random random = new Random(0);

			//tigers.Add(CreateTiger(1.0f, 0.0f, Vector3.Zero));

			// Tiger erstellen
			for (int i = -1; i < 2; i++)
				for (int j = -1; j < 2; j++)
				{
					float scale = ((float)random.NextDouble() + 1.0f) / 2.0f;
					float rotation = (float)random.NextDouble() * MathHelper.TwoPi;
					Vector3 position = new Vector3(i, 0.0f, j) * 3.0f;

					tigers.Add(CreateTiger(scale, rotation, position));
				}
		}


		// erstellt einen Tiger mit angegebener Skalierung, Rotation und Position
		private Tiger CreateTiger(float scale, float rotate, Vector3 position)
		{
			return new Tiger(
				Matrix.CreateTranslation(Vector3.Up * 0.7f) * 
				Matrix.CreateScale(scale) * 
				Matrix.CreateRotationY(rotate) * 
				Matrix.CreateTranslation(position));
		}


		protected override void LoadContent()
		{
			// Tiger-Model und Textur laden
			model = Game.Content.Load<Model>("Models/Tiger/Tiger");
			texture = Game.Content.Load<Texture2D>("Models/Tiger/Texture");

			base.LoadContent();
		} 


		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}


		public override void Draw(GameTime gameTime)
		{
			// Texture setzen
			Effect.Parameters["TextureEnabled"].SetValue(true);
			Effect.Parameters["Texture"].SetValue(texture);

			// für alle Tiger
			foreach (Tiger tiger in tigers)
			{
				// WorldMatrix setzen
				Effect.Parameters["World"].SetValue(tiger.World);

				// Tiger zeichnen
				foreach (ModelMesh mesh in model.Meshes)
				{
					foreach (ModelMeshPart part in mesh.MeshParts)
						part.Effect = Effect;

					mesh.Draw();
				}
			}

			base.Draw(gameTime);
		}
	}
}
