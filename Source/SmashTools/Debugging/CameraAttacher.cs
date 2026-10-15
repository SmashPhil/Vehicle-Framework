using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;
using Verse;
using RimWorld;

namespace SmashTools
{
	[StaticConstructorOnStartup]
	public class CameraAttacher : MonoBehaviour
	{
		private Thing thing;

		private static GameObject currentAttacher;

		private void Update()
		{
			if (!thing.Spawned || Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
			{
				CameraController.Close();
				Destroy(gameObject);
			}
			else
			{
				CameraController.Update(thing.DrawPos);
			}
		}

		public static CameraAttacher Create(Thing thing)
		{
			if (currentAttacher)
			{
				Destroy(currentAttacher);
				CameraController.Close();
			}
			CameraController.Start(Find.Camera);
			GameObject gameObject = new GameObject("CameraAttacher", typeof(CameraAttacher));
			CameraAttacher cameraAttacher = gameObject.GetComponent<CameraAttacher>();
			cameraAttacher.thing = thing;

			currentAttacher = gameObject;

			return cameraAttacher;
		}
	}
}
