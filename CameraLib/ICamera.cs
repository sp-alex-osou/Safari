using System;

using Microsoft.Xna.Framework;

namespace CameraLib
{
	public interface ICamera : IGameComponent, IUpdateable, ICameraService
	{
		new Vector3 Position { get; set; }

		void Look(Vector3 direction);
		void LookAt(Vector3 target);
		
		void Pitch(float angle);
		void Yaw(float angle);
		void Roll(float angle);

		void Rotate(Vector3 axis, float angle);
		void Rotate(Quaternion rotation);
	}
}
