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
	public interface ICameraService
	{
		Vector3 Position { get; }

		Vector3 Forward { get; }
		Vector3 Up { get; }
		Vector3 Right { get; }

		Vector3 MoveForward { get; }
		Vector3 MoveUp { get; }
		Vector3 MoveRight { get; }

		BoundingFrustum ViewFrustum { get; }

		Matrix View { get; }
		Matrix Projection { get; }

		float FieldOfView { get; }
		float NearPlaneDistance { get; }
		float FarPlaneDistance { get; }
	}
}