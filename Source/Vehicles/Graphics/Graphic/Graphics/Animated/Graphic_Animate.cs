using UnityEngine;
using Verse;
using SmashTools;

namespace Vehicles
{
	public class Graphic_Animate : Graphic_Collection
	{
		public int AnimationCount => subGraphics?.Length ?? 0;

		public override Material MatSingle => subGraphics[0].MatSingle;

		public override Graphic GetColoredVersion(Shader newShader, Color newColor, Color newColorTwo)
		{
			return GraphicDatabase.Get<Graphic_Animate>(path, newShader, drawSize, newColor, newColorTwo, data);
		}

		public void DrawWorkerAnimated(Thing thing, int index, float extraRotation, bool rotatePoints = false)
		{
			Mesh mesh = MeshAt(thing.Rotation);
			Quaternion quaternion = QuatFromRot(thing.Rotation);
			if (extraRotation != 0f)
			{
				quaternion *= Quaternion.Euler(Vector3.up * extraRotation);
			}
			Vector3 offset = DrawOffset(thing.Rotation);
			if (rotatePoints && extraRotation != 0)
			{
				offset = Ext_Math.RotatePoint(offset, Vector3.zero, extraRotation);
			}
			Vector3 newLoc = thing.DrawPos;
			newLoc += offset;

			Material mat = SubGraphicForIndex(index).MatSingle;
			DrawMeshInt(mesh, newLoc, quaternion, mat);
			if (ShadowGraphic != null)
			{
				ShadowGraphic.DrawWorker(newLoc, thing.Rotation, null, null, extraRotation);
			}
		}

		public void DrawWorkerAnimated(Vector3 loc, Rot4 rot, int index, float extraRotation, bool rotatePoints = false)
		{
			Mesh mesh = MeshAt(rot);
			Vector3 newLoc = loc;
			Quaternion quaternion = QuatFromRot(rot);
			if (extraRotation != 0f)
			{
				quaternion *= Quaternion.Euler(Vector3.up * extraRotation);
			}
			Vector3 offset = DrawOffset(rot);
			if (rotatePoints && extraRotation != 0)
			{
				offset = Ext_Math.RotatePoint(offset, Vector3.zero, extraRotation);
			}
			newLoc += offset;

			Material mat = SubGraphicForIndex(index).MatSingle;
			DrawMeshInt(mesh, newLoc, quaternion, mat);
			if (ShadowGraphic != null)
			{
				ShadowGraphic.DrawWorker(newLoc, rot, null, null, extraRotation);
			}
		}

		public override Material MatAt(Rot4 rot, Thing thing = null)
		{
			if (thing is null)
			{
				return MatSingle;
			}
			return MatSingleFor(thing);
		}

		public virtual Material MatAt(Rot4 rot, int index)
		{
			return SubGraphicForIndex(index).MatSingle;
		}

		public Graphic SubGraphicForIndex(int index)
		{
			index %= subGraphics.Length;
			return subGraphics[index];
		}
	}
}
