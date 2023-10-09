using UnityEngine;

namespace Vehicles
{
	public class MoteThrownSlowToSpeed : MoteThrownExpand
	{
		public Vector3 deceleration;
		public Vector3 minDeceleration;
		protected bool xNeg = false;
		protected bool zNeg = false;
		protected bool velocityXNeg = false;
		protected bool velocityZNeg = false;

		public Vector3 DecelerationLerp
		{
			get
			{
				float t = Mathf.Clamp01(def.mote.Lifespan - AgeSecs);
				float multiplier = Mathf.Pow(t, 2.5f);
				Vector3 decelAtT = deceleration * multiplier;
				Vector3 decelToZero = new Vector3(xNeg ? Mathf.Min(minDeceleration.x, decelAtT.x) : Mathf.Max(minDeceleration.x, decelAtT.x), decelAtT.y, zNeg ? Mathf.Min(minDeceleration.z, decelAtT.z) : Mathf.Max(minDeceleration.z, decelAtT.z));
				return decelToZero;
			}
		}

		public void SetDecelerationRate(float rate, float fixedAcceleration, float angle)
		{
			deceleration = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * rate;
			minDeceleration = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * fixedAcceleration;
			xNeg = deceleration.x < 0;
			zNeg = deceleration.z < 0;
			velocityXNeg = velocity.x < 0;
			velocityZNeg = velocity.z < 0;
		}

		protected override void TimeInterval(float deltaTime)
		{
			base.TimeInterval(deltaTime);
			velocity += DecelerationLerp * deltaTime;
			velocity.x = velocityXNeg ? Mathf.Min(0, velocity.x) : Mathf.Max(0, velocity.x);
			velocity.z = velocityZNeg ? Mathf.Min(0, velocity.z) : Mathf.Max(0, velocity.z);
		}
	}
}
