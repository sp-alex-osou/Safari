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

namespace CameraLib
{
	public class CameraHandler : GameComponent
	{
		public ICamera Camera { get; set; }

		public float MouseSpeed { get; set; }
		public float RotationSpeed { get; set; }
		public float MovementSpeed { get; set; }
		public float MovementBoost { get; set; }

		KeyboardState keyboardState;
		KeyboardState prevKeyboardState;

		Point center;
		bool ignoreMouse;

		public CameraHandler(Game game, ICamera camera)
			: base(game)
		{
			Camera = camera;

			RotationSpeed = 1.0f;
			MouseSpeed = 0.1f;
			MovementSpeed = 10.0f;
			MovementBoost = 10.0f;

			Game.Window.ClientSizeChanged += new EventHandler<EventArgs>(Window_ClientSizeChanged);
		}

		void Window_ClientSizeChanged(object sender, EventArgs e)
		{
			InitializeMouse();
		}

		public override void Initialize()
		{
			InitializeMouse();

			base.Initialize();
		}

		private void InitializeMouse()
		{
			center.X = Game.Window.ClientBounds.Width / 2;
			center.Y = Game.Window.ClientBounds.Height / 2;

			Mouse.SetPosition(center.X, center.Y);
		}

		public override void Update(GameTime gameTime)
		{
			if (Camera == null || !Game.IsActive)
				return;

			UpdateKeyboard(gameTime);
			UpdateMouse(gameTime);

			base.Update(gameTime);
		}

		private void UpdateKeyboard(GameTime gameTime)
		{
			float elapsed = GetElapsed(gameTime);

			float movementSpeed = MovementSpeed * elapsed;
			float rotationSpeed = RotationSpeed * elapsed;

			Vector3 direction = Vector3.Zero;

			keyboardState = Keyboard.GetState();
			Keys[] keys = keyboardState.GetPressedKeys();

			foreach (Keys key in keys)
				switch (key)
				{
					case Keys.W: direction += Camera.MoveForward; break;
					case Keys.S: direction -= Camera.MoveForward; break;
					case Keys.D: direction += Camera.MoveRight; break;
					case Keys.A: direction -= Camera.MoveRight; break;
					case Keys.Y: direction += Camera.MoveUp; break;
					case Keys.X: direction -= Camera.MoveUp; break;
					case Keys.E: Camera.Roll(rotationSpeed); break;
					case Keys.Q: Camera.Roll(-rotationSpeed); break;
					case Keys.Up: Camera.Pitch(rotationSpeed); break;
					case Keys.Down: Camera.Pitch(-rotationSpeed); break;
					case Keys.Left: Camera.Yaw(rotationSpeed); break;
					case Keys.Right: Camera.Yaw(-rotationSpeed); break;
				}

			if (direction != Vector3.Zero)
			{
				Vector3 velocity = Vector3.Normalize(direction) * movementSpeed;

				if (keyboardState.IsKeyDown(Keys.LeftControl))
					velocity *= MovementBoost;

				Camera.Position += velocity;

				if (Camera.Position.Y < 0.2f)
					Camera.Position = new Vector3(Camera.Position.X, 0.2f, Camera.Position.Z);
			}

			if (IsKeyTyped(Keys.LeftShift))
			{
				ignoreMouse = !ignoreMouse;
				Game.IsMouseVisible = ignoreMouse;

				if (!ignoreMouse)
					InitializeMouse();
			}

			prevKeyboardState = keyboardState;
		}

		private void UpdateMouse(GameTime gameTime)
		{
			float elapsed = GetElapsed(gameTime);

			if (ignoreMouse)
				return;

			float mouseSpeed = MouseSpeed * elapsed;

			MouseState mouseState = Mouse.GetState();

			if (mouseState.X != center.X)
				Camera.Yaw((center.X - mouseState.X) * mouseSpeed);

			if (mouseState.Y != center.Y)
				Camera.Pitch((center.Y - mouseState.Y) * mouseSpeed);

			Mouse.SetPosition(center.X, center.Y);
		}

		private float GetElapsed(GameTime gameTime)
		{
			return (float)gameTime.ElapsedGameTime.TotalSeconds;
		}

		private bool IsKeyTyped(Keys key)
		{
			return prevKeyboardState.IsKeyUp(key) && keyboardState.IsKeyDown(key);
		}
	}
}