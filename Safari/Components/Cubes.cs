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
	public class Cubes : DrawableGameComponent
	{
		List<Cube> cubes;

		Vector3[] positions;
		int[][] triangles;

		VertexBuffer vertexBuffer;
		IndexBuffer indexBuffer;

		BasicEffect effect;
		ICameraService camera;

		Matrix[] transformations;

		Random random;

		VertexPositionColor[] triangle;

		bool intersection;

		SpriteBatch spriteBatch;
		Texture2D crosshair;

		struct Cube
		{
			public BoundingBox BoundingBox;
			public List<Matrix> Surfaces;
			public Vector3 Position;
		}
		
		public Cubes(Game game) : base(game)
		{
			cubes = new List<Cube>();
			random = new Random(0);
		}


		public override void Initialize()
		{
			base.Initialize();

			// Grund-Positionen für die Ecken einer Seite eines Würfels
			positions = new Vector3[4] 
			{
				new Vector3(-1.0f, -1.0f, 0.0f),
				new Vector3(-1.0f, 1.0f, 0.0f),
				new Vector3(1.0f, 1.0f, 0.0f),
				new Vector3(1.0f, -1.0f, 0.0f)
			};

			// Indices für die zwei Dreiecke einer Seite
			triangles = new int[2][]
			{
				new int[3] { 0, 1, 2 },
				new int[3] { 2, 3, 0 }
			};

			// rotes Dreieck für die Anzeige des geschnittenen Dreiecks
			triangle = new VertexPositionColor[3];

			for (int i = 0; i < 3; i++)
				triangle[i].Color = Color.Red;

			// vier Vertices zum Rendern einer Seite eines Würfels
			Vertex[] vertices = new Vertex[4];

			for (int i = 0; i < 4; i++)
			{
				vertices[i].Position = positions[i];
				vertices[i].Normal = Vector3.Backward;
			}

			// eine Transformationsmatrix pro Seite eines Würfels
			transformations = new Matrix[6];

			for (int i = 0; i < 6; i++)
			{
				transformations[i] = Matrix.CreateTranslation(Vector3.Backward);

				if (i < 4)
					transformations[i] *= Matrix.CreateRotationY(i * MathHelper.PiOver2);
				else
					transformations[i] *= Matrix.CreateRotationX(((i - 4) * 2 - 1) * MathHelper.PiOver2);
			}

			effect = new BasicEffect(GraphicsDevice);

			vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(Vertex), 4, BufferUsage.WriteOnly);
			indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, 4, BufferUsage.WriteOnly);

			// Vertex- und Indexbuffer zum Rendern einer Seite eines Würfels
			vertexBuffer.SetData<Vertex>(vertices);
			indexBuffer.SetData<int>(new int[4] { 0, 1, 3, 2 });

			camera = (ICameraService)Game.Services.GetService(typeof(ICameraService));

			Random random = new Random(0);

			// Würfel zufällig generieren
			for (int i = 0; i < 100; i++)
				AddCube((new Vector3(Next(), Next(), Next()) * 2.0f - Vector3.One) * 100.0f, Next() * 10.0f);

			spriteBatch = new SpriteBatch(GraphicsDevice);
		}


		protected override void LoadContent()
		{
			// Fadenkreuz-Textur laden
			crosshair = Game.Content.Load<Texture2D>("Textures/Crosshair");

			base.LoadContent();
		}


		private float Next()
		{
			return (float)random.NextDouble();
		}


		public override void Draw(GameTime gameTime)
		{
			// Culling deaktivieren, Blending aktivieren
			GraphicsDevice.RasterizerState = RasterizerState.CullNone;
			GraphicsDevice.BlendState = BlendState.AlphaBlend;

			// Vertex- und Indexbuffer setzen
			GraphicsDevice.SetVertexBuffer(vertexBuffer);
			GraphicsDevice.Indices = indexBuffer;

			// View und Projectionsmatrix im Shader initialisieren
			effect.View = camera.View;
			effect.Projection = camera.Projection;

			foreach (EffectPass pass in effect.CurrentTechnique.Passes)
			{
				// Update des Tiefenpuffers deaktivieren (um Z-Fight bei rotem Dreieck zu vermeiden)
				GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

				// 50% Transparenz
				effect.Alpha = 0.5f;

				// für alle Seiten aller Würfel, geordnet nach Entfernung zur Kamera
				foreach (Matrix surface in cubes.SelectMany(cu => cu.Surfaces).OrderByDescending(s => (s.Translation - camera.Position).LengthSquared()))
				{				
					effect.EnableDefaultLighting();
					effect.VertexColorEnabled = false;
					effect.World = surface;
					pass.Apply();

					// Seite zeichnen
					GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleStrip, 0, 0, 4, 0, 2);
				}

				// wenn eine Intersection aufgetreten ist, rotes Dreieck zeichnen
				if (intersection)
				{
					effect.World = Matrix.Identity;
					effect.VertexColorEnabled = true;
					effect.LightingEnabled = false;
					effect.Alpha = 1.0f;
					pass.Apply();

					GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.TriangleList, triangle, 0, 1);
				}
			}

			Viewport vp = GraphicsDevice.Viewport;

			// Fadenkreuz zeichnen
			spriteBatch.Begin();
			spriteBatch.Draw(crosshair, new Rectangle(vp.Width / 2 - 10, vp.Height / 2 - 10, 20, 20), Color.White);
			spriteBatch.End();

			// GraphicsDevice States auf Default setzen
			GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
			GraphicsDevice.BlendState = BlendState.Opaque;
			GraphicsDevice.DepthStencilState = DepthStencilState.Default;

			base.Draw(gameTime);
		}


		// fügt einen Würfel hinzu
		private void AddCube(Vector3 position, float scale)
		{
			// Würfel initialisieren
			Cube cube = new Cube() 
			{ 
				BoundingBox = new BoundingBox(position - Vector3.One * scale, position + Vector3.One * scale),
				Surfaces = new List<Matrix>(),
				Position = position
			};

			// Seiten des Würfels erstellen
			for (int i = 0; i < 6; i++)
				cube.Surfaces.Add(transformations[i] * Matrix.CreateScale(scale) * Matrix.CreateTranslation(position));

			cubes.Add(cube);
		}


		// sucht nach einem Schnittpunkt zwischen Würfel und Strahl
		public float? FindIntersection(Ray ray)
		{
			intersection = false;

			// für alle Würfel, sortiert nach Entfernung zum Startpunkt des Strahls
			foreach (Cube cube in cubes.OrderBy(c => (c.Position - ray.Position).LengthSquared()))
			{
				// wenn Strahl die BoundingBox des Würfels schneidet
				if (ray.Intersects(cube.BoundingBox) != null)
				{
					// für alle Seiten des Würfels, sortiert nach Entfernung zum Startpunkt des Strahls
					foreach (Matrix surface in cube.Surfaces.OrderBy(s => (s.Translation - ray.Position).LengthSquared()))
					{
						// für alle Dreiecke der Seite
						foreach (int[] indices in triangles)
						{
							// Dreiecks-Positionen ermitteln
							Vector3[] triangle = GetTriangle(indices, surface);

							// auf Intersection zwischen Strahl und Dreieck testen
							float? f = Intersects(ray, triangle);

							// wenn Intersection, rotes Dreieck setzen und Entfernung returnen
							if (f != null)
							{
								intersection = true;

								for (int i = 0; i < 3; i++)
									this.triangle[i].Position = triangle[i];

								return f;
							}
						}
					}
				}
			}

			return null;
		}


		// berechnet die Positionen der Eckpunkte des Dreiecks
		private Vector3[] GetTriangle(int[] indices, Matrix matrix)
		{
			return new Vector3[3]
			{
				Vector3.Transform(positions[indices[0]], matrix),
				Vector3.Transform(positions[indices[1]], matrix),
				Vector3.Transform(positions[indices[2]], matrix)
			};
		}


		// überprüft, ob ein Punkt in einem Dreieck liegt
		private bool IsInsideTriangle(Vector3[] t, Vector3 p)
		{
			// Up-Vektor des Dreiecks mit Cross-Product berechnen
			Vector3 n = Vector3.Normalize(Vector3.Cross(t[1] - t[0], t[2] - t[0]));

			// für alle Seiten des Dreiecks
			for (int i = 0; i < 3; i++)
			{
				// wenn Punkt außerhalb liegt, Überprüfung beenden
				if (Vector3.Dot(Vector3.Normalize(Vector3.Cross(t[i] - p, t[(i + 1) % 3] - p)), n) < 0.0f)
					return false;
			}

			return true;
		}


		// überprüft, ob ein Strahl ein Dreieck schneidet
		private float? Intersects(Ray ray, Vector3[] triangle)
		{
			// überprüfen, ob der Strahl die vom Dreieck aufgespannte Ebene schneidet
			float? f = ray.Intersects(new Plane(triangle[0], triangle[1], triangle[2]));

			// wenn ja, überprüfen ob Schnittpunkt im Dreieck liegt
			if (f != null && IsInsideTriangle(triangle, ray.Position + ray.Direction * (float)f))
				return f;

			return null;
		}
	}
}
