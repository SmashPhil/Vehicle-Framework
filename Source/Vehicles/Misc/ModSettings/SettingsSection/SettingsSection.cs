using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.Sound;
using RimWorld;
using SmashTools;
using UnityEngine;

namespace Vehicles
{
	public abstract class SettingsSection : IExposable
	{
		protected static Listing_Standard listingStandard = new Listing_Standard();
		protected static Listing_Settings listingSplit = new Listing_Settings();

		public virtual IEnumerable<FloatMenuOption> ResetOptions
		{
			get
			{
				yield return new FloatMenuOption("DevModeResetPage".Translate(), () => ResetSettings());

				yield return new FloatMenuOption("DevModeResetAll".Translate(), delegate ()
				{
					VehicleMod.ResetAllSettings();
				});
			}
		}

		public virtual Rect ButtonRect(Rect rect) => new Rect(rect.x + 2.5f, rect.y - 2.5f, rect.width, rect.height);

		public virtual void ResetSettings()
		{
			SoundDefOf.Click.PlayOneShotOnCamera(null);
		}

		public abstract void DrawSection(Rect rect);

		public virtual void Initialize()
		{
		}

		public virtual void ExposeData()
		{
		}

		public virtual void PostDefDatabase()
		{
		}

		public virtual void VehicleSelected()
		{
		}
	}
}
