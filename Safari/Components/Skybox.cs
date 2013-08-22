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

using Vertex = Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture;


namespace Safari.Components
{
	public class Skybox : DrawableGameComponent
	{
		public BasicEffect Effect { get; set; }

		int numVertices;
		int numTriangles;
		Vertex[] vertices;
		int[] indices;
		Texture2D[] textures;


		public Skybox(Game game) : base(game)
		{
			textures = new Texture2D[6];
			numVertices = 4;
			numTriangles = 2;
			indices = new int[] { 0, 1, 2, 2, 3, 0 };
			vertices = new Vertex[4];
		}


		public override void Initialize()
		{
			base.Initialize();

			Effect = new BasicEffect(Game.GraphicsDevice);

			// Vertices zum Rendern einer Fläche der Skybox initialisieren
			vertices[0].TextureCoordinate = new Vector2(0.0f, 1.0f);
			vertices[0].Position = new Vector3(-1.0f, -1.0f, -1.0f);
			vertices[1].TextureCoordinate = new Vector2(0.0f, 0.0f);
			vertices[1].Position = new Vector3(-1.0f, 1.0f, -1.0f);
			vertices[2].TextureCoordinate = new Vector2(1.0f, 0.0f);
			vertices[2].Position = new Vector3(1.0f, 1.0f, -1.0f);
			vertices[3].TextureCoordinate = new Vector2(1.0f, 1.0f);
			vertices[3].Position = new Vector3(1.0f, -1.0f, -1.0f);

			// Normalvektoren setzen
			for (int i = 0; i < 4; i++)
				vertices[i].Normal = Vector3.Backward;
		}


		protected override void LoadContent()
		{
			// Skybox-Texturen laden
			for (int i = 0; i < 6; i++)
				textures[i] = Game.Content.Load<Texture2D>("Textures/Skybox/" + (i + 1));

			base.LoadContent();
		}


		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}


		public override void Draw(GameTime gameTime)
		{
			var camera = (ICameraService)Game.Services.GetService(typeof(ICameraService));

			Vector3 scale, translation;
			Quaternion rotation;

			// Rotation aus der ViewMatrix ermitteln
			camera.View.Decompose(out scale, out rotation, out translation);

			Effect.View = Matrix.CreateFromQuaternion(rotation);
			Effect.Projection = camera.Projection;

			Effect.TextureEnabled = true;

			// Tiefentest deaktivieren
			GraphicsDevice.DepthStencilState = DepthStencilState.None;
			GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;

			foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
			{
				// für alle Fläche der Skybox
				for (int i = 0; i < 6; i++)
				{
					// Textur auswählen
					Effect.Texture = textures[i];

					// WorldMatrix berechnen
					if (i < 4)
						Effect.World = Matrix.CreateRotationY(i * -MathHelper.PiOver2);
					else if (i == 4)
						Effect.World = Matrix.CreateRotationX(-MathHelper.PiOver2);
					else
						Effect.World = Matrix.CreateRotationX(MathHelper.PiOver2);

					pass.Apply();

					// Fläche zeichnen
					GraphicsDevice.DrawUserIndexedPrimitives<Vertex>(
						PrimitiveType.TriangleList, vertices, 0, numVertices, indices, 0, numTriangles);
				}

			}

			GraphicsDevice.DepthStencilState = DepthStencilState.Default;
			GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

			base.Draw(gameTime);
		}
	}
}
