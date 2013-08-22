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

namespace Safari.Components
{
	public class Line : DrawableGameComponent
	{
		public Vector3 Start { get; set; }
		public Vector3 End { get; set; }

		ICameraService camera;

		BasicEffect effect;

		VertexPositionColor[] vertices;


		public Line(Game game) : base(game)
		{
		}


		public override void Initialize()
		{
			base.Initialize();

			camera = (ICameraService)Game.Services.GetService(typeof(ICameraService));
			effect = new BasicEffect(Game.GraphicsDevice);

			vertices = new VertexPositionColor[2];

			for (int i = 0; i < 2; i++)
				vertices[i].Color = Color.Black;
		}


		public override void Draw(GameTime gameTime)
		{
			effect.World = Matrix.Identity;
			effect.View = camera.View;
			effect.Projection = camera.Projection;
			effect.VertexColorEnabled = true;

			if (Start != End)
			{
				vertices[0].Position = Start;
				vertices[1].Position = End;

				foreach (EffectPass pass in effect.CurrentTechnique.Passes)
				{
					pass.Apply();

					GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(PrimitiveType.LineList, vertices, 0, 1);
				}
			}

			base.Draw(gameTime);
		}
	}
}
