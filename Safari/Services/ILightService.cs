using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Safari.Services
{
	public interface ILightService
	{
		Vector3[] GetPositions();
		Matrix[] GetViewProjections();

		int Count { get; }
	}
}
