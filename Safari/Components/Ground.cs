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
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using CameraLib;
using Safari.Services;

using Vertex = Microsoft.Xna.Framework.Graphics.VertexPositionNormalTexture;

namespace Safari.Components
{
	public class Ground : DrawableGameComponent
	{
		public Effect Effect { get; set; }
		public bool NormalMapEnabled { get; set; }

		int numVertices;
		int numIndices;
		int[] indices;
		Vertex[] vertices;

		VertexBuffer vertexBuffer;
		IndexBuffer indexBuffer;

		Texture2D texture;
		Texture2D normalMap;

		int width = 200;
		int height = 200;

		int totalWidth = 1000;
		int totalHeight = 1000;

		float texRes = 0.1f;

		public Ground(Game game) : base(game)
		{
			NormalMapEnabled = true;
		}


		public override void Initialize()
		{
			base.Initialize();

			// Vertices und Indices erstellen
			numVertices = (width + 1) * (height + 1);
			numIndices = width * height * 6;

			indices = new int[numIndices];
			vertices = new Vertex[numVertices];

			// Vertices initialisieren
			for (int i = 0; i < width + 1; i++)
				for (int j = 0; j < height + 1; j++)
				{
					vertices[GetIndex(i, j)] = new Vertex()
					{
						Position = new Vector3(i, 0.0f, -j),
						Normal = Vector3.Up,
						TextureCoordinate = new Vector2(i * texRes * totalWidth / width, j * texRes * totalHeight / height)
					};
				}

			int count = 0;

			// Indices initialisieren
			for (int i = 0; i < width; i++)
				for (int j = 0; j < height; j++)
				{
					indices[count++] = GetIndex(i, j);
					indices[count++] = GetIndex(i, j+1);
					indices[count++] = GetIndex(i+1, j+1);

					indices[count++] = GetIndex(i+1, j+1);
					indices[count++] = GetIndex(i+1, j);
					indices[count++] = GetIndex(i, j);
				}

			// Vertex- und Indexbuffer ertellen und initialisieren
			vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(Vertex), numVertices, BufferUsage.WriteOnly);
			indexBuffer = new IndexBuffer(GraphicsDevice, typeof(int), numIndices, BufferUsage.WriteOnly);

			vertexBuffer.SetData<Vertex>(vertices);
			indexBuffer.SetData<int>(indices);
		}


		// liefert den Vertex-Index der angegebenen Koordinaten
		private int GetIndex(int x, int y)
		{
			return x + y * (width + 1);
		}


		protected override void LoadContent()
		{
			// Textur und NormalMap laden
			texture = Game.Content.Load<Texture2D>("Textures\\Desert");
			normalMap = Game.Content.Load<Texture2D>("Textures\\NormalMap");

			base.LoadContent();
		}


		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);
		}


		public override void Draw(GameTime gameTime)
		{
			base.Draw(gameTime);

			// Boden zentrieren
			Matrix world = 
				Matrix.CreateTranslation(-width / 2.0f, 0, height / 2.0f) * 
				Matrix.CreateScale(totalWidth / width, 1.0f, totalHeight / height);

			// Shader Parameter setzen
			Effect.Parameters["TextureEnabled"].SetValue(true);
			Effect.Parameters["Texture"].SetValue(texture);
			Effect.Parameters["NormalMapEnabled"].SetValue(NormalMapEnabled);
			Effect.Parameters["NormalMap"].SetValue(normalMap);
			Effect.Parameters["World"].SetValue(world);

			// Vertex- und Indexbuffer setzen
			GraphicsDevice.SetVertexBuffer(vertexBuffer);
			GraphicsDevice.Indices = indexBuffer;

			// Boden zeichnen
			foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
			{
				pass.Apply();

				GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVertices, 0, numIndices / 3);
			}

			Effect.Parameters["NormalMapEnabled"].SetValue(false);
		}
	}
}