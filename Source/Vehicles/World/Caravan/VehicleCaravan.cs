using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse;
using SmashTools;

namespace Vehicles
{
	[StaticConstructorOnStartup]
	public class VehicleCaravan : Caravan, IVehicleWorldObject
	{
		private const int RepairMothballTicks = 300;

		private static readonly Texture2D SplitCommand = ContentFinder<Texture2D>.Get("UI/Commands/SplitCaravan", true);
		private static readonly Dictionary<VehicleDef, int> vehicleCounts = new Dictionary<VehicleDef, int>();

		private static MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();
		private static Dictionary<ThingDef, Material> materials = new Dictionary<ThingDef, Material>();

		public VehicleCaravan_PathFollower vehiclePather;
		public VehicleCaravan_Tweener vehicleTweener;

		private VehiclePawn leadVehicle;
		private bool initialized;

		private bool repairing;

		private List<VehiclePawn> vehicles = new List<VehiclePawn>();

		public VehicleCaravan() : base()
		{
			vehiclePather = new VehicleCaravan_PathFollower(this);
			vehicleTweener = new VehicleCaravan_Tweener(this);
		}

		public float ConstructionAverage { get; private set; }

		public bool VehiclesNeedRepairs { get; private set; }

		public override Vector3 DrawPos => vehicleTweener.TweenedPos;

		public bool CanDismount => true;

		public bool AerialVehicle => vehicles.Count == 1 && vehicles.FirstOrDefault().VehicleDef.vehicleType == VehicleType.Air;

		public bool CanCaravan => vehicles.Count == 1 && !vehicles.FirstOrDefault().VehicleDef.canCaravan;

		public IEnumerable<VehiclePawn> Vehicles => vehicles;

		public List<VehiclePawn> VehiclesListForReading => vehicles;

		public IEnumerable<Pawn> DismountedPawns
		{
			get
			{
				foreach (Pawn pawn in PawnsListForReading)
				{
					if (!(pawn is VehiclePawn) && !pawn.IsInVehicle())
					{
						yield return pawn;
					}
				}
			}
		}

		public bool Repairing
		{
			get
			{
				if (vehiclePather.Moving)
				{
					return false;
				}
				return repairing;
			}
			set
			{
				repairing = value;
			}
		}

		public VehiclePawn LeadVehicle
		{
			get
			{
				if (leadVehicle is null)
				{
					leadVehicle = PawnsListForReading.FirstOrDefault(v => v is VehiclePawn) as VehiclePawn;
				}
				return leadVehicle;
			}
		}

		public override Material Material
		{
			get
			{
				VehicleDef leadVehicleDef = LeadVehicle?.VehicleDef;
				if (leadVehicleDef is null)
				{
					return null;
				}
				if (!materials.ContainsKey(leadVehicleDef))
				{
					Texture2D texture = VehicleTex.CachedTextureIcons[leadVehicleDef];
					Material material = MaterialPool.MatFrom(texture, ShaderDatabase.WorldOverlayTransparentLit, Color.white, WorldMaterials.WorldObjectRenderQueue);
					materials.Add(leadVehicleDef, material);
				}
				return materials[leadVehicleDef];
			}
		}

		public bool CanLaunch
		{
			get
			{
				foreach (VehiclePawn vehicle in vehicles)
				{
					if (vehicle.CompVehicleLauncher == null)
					{
						return false;
					}
				}
				return true;
			}
		}

		public bool OutOfFuel
		{
			get
			{
				foreach (VehiclePawn vehicle in vehicles)
				{
					if (vehicle.CompFueledTravel != null && vehicle.CompFueledTravel.Fuel <= 0)
					{
						return true;
					}
				}
				return false;
			}
		}

		public new int TicksPerMove
		{
			get
			{
				return VehicleCaravanTicksPerMoveUtility.GetTicksPerMove(this, null);
			}
		}

		public new string TicksPerMoveExplanation
		{
			get
			{
				StringBuilder stringBuilder = new StringBuilder();
				VehicleCaravanTicksPerMoveUtility.GetTicksPerMove(this, stringBuilder);
				return stringBuilder.ToString();
			}
		}

		public override void Draw()
		{
			float averageTileSize = Find.WorldGrid.averageTileSize;
			float transitionPct = ExpandableWorldObjectsUtility.TransitionPct;
			if (def.expandingIcon && transitionPct > 0f)
			{
				Color color = Material.color;
				float num = 1f - transitionPct;
				propertyBlock.SetColor(ShaderPropertyIDs.Color, new Color(color.r, color.g, color.b, color.a * num));
				WorldHelper.DrawQuadTangentialToPlanet(DrawPos, 0.7f * averageTileSize, 0.015f, Material, propertyBlock: propertyBlock);
				return;
			}
			WorldHelper.DrawQuadTangentialToPlanet(DrawPos, 0.7f * averageTileSize, 0.015f, Material);
		}

		public void DrawQuadTangentialToPlanet(Vector3 pos, float size, float altOffset, Material material, bool counterClockwise = false, bool useSkyboxLayer = false, MaterialPropertyBlock propertyBlock = null)
		{
			if (material == null)
			{
				Log.Warning("Tried to draw quad with null material.");
				return;
			}
			Vector3 normalized = pos.normalized;
			Vector3 vector;

			Vector2 drawSize = new Vector2(LeadVehicle.VehicleDef.graphicData.drawSize.x, LeadVehicle.VehicleDef.graphicData.drawSize.y);

			if (counterClockwise)
			{
				vector = -normalized;
			}
			else
			{
				vector = normalized;
			}
			int smallerSide = drawSize.x < drawSize.y ? -1 : 1;
			float vehicleSizeX;
			float vehicleSizeY;

			float ratio;

			if (smallerSide == 1)
			{
				ratio = drawSize.x / size;

				vehicleSizeX = size;
				vehicleSizeY = drawSize.y / ratio;
			}
			else
			{
				ratio = drawSize.y / size;

				vehicleSizeX = drawSize.x / ratio;
				vehicleSizeY = size;
			}

			Quaternion q = Quaternion.LookRotation(Vector3.Cross(vector, Vector3.up), vector) * Quaternion.Euler(0, -90f, 0);
			//Swapped X and Y due to using Rot4.West
			//Vector3 s = new Vector3(vehicleSizeY, 1f, vehicleSizeX); 
			Vector3 s = new Vector3(size, 1f, size);
			Matrix4x4 matrix = default;
			matrix.SetTRS(pos + normalized * altOffset, q, s);
			int layer = useSkyboxLayer ? WorldCameraManager.WorldSkyboxLayer : WorldCameraManager.WorldLayer;
			if (propertyBlock != null)
			{
				Graphics.DrawMesh(MeshPool.plane10, matrix, material, layer, null, 0, propertyBlock);
				//Graphics.DrawMesh(MeshPool.plane10, matrix, LeadVehicle.VehicleGraphic.MatAt(Rot4.West, LeadVehicle), layer, null, 0, propertyBlock);
				return;
			}
			Graphics.DrawMesh(MeshPool.plane10, matrix, material, layer);
			//Graphics.DrawMesh(MeshPool.plane10, matrix, LeadVehicle.VehicleGraphic.MatAt(Rot4.West, LeadVehicle), layer);
			//if (LeadVehicle.CompCannons != null)
			//{
			//    Vector3 cPos = pos;

			//    foreach (VehicleTurret cannon in LeadVehicle.CompCannons.Cannons)
			//    {
			//        cPos.y += 0.1f;
			//        Vector3 s2 = new Vector3(cannon.turretDef.graphicData.drawSize.x / ratio, 1f, cannon.turretDef.graphicData.drawSize.y / ratio);
			//        Quaternion q2 = Quaternion.LookRotation(Vector3.Cross(vector, Vector3.up), vector) * Quaternion.Euler(0, cannon.defaultAngleRotated, 0);
			//        matrix.SetTRS(cPos + normalized * altOffset, q2, s2);
			//        Graphics.DrawMesh(MeshPool.plane10, matrix, cannon.CannonMaterial, layer);
			//    }
			//}
		}

		public override IEnumerable<Gizmo> GetGizmos()
		{
			foreach (Gizmo gizmo in base.GetGizmos())
			{
				if (gizmo is Command command && command.icon == null)
				{
					continue; //skip all vanilla caravan devmode commands
				}
				yield return gizmo;
			}

			if (IsPlayerControlled)
			{
				if (AerialVehicle)
				{
					VehiclePawn vehicle = vehicles.FirstOrDefault();
					Command_Action launchCommand = new Command_Action()
					{
						defaultLabel = "CommandLaunchGroup".Translate(),
						defaultDesc = "CommandLaunchGroupDesc".Translate(),
						icon = VehicleTex.LaunchCommandTex,
						alsoClickIfOtherInGroupClicked = false,
						action = delegate ()
						{
							LaunchTargeter.BeginTargeting(vehicle, (GlobalTargetInfo target, float fuelCost) => AerialVehicleLaunchHelper.ChoseTargetOnMap(vehicle, Tile, target, fuelCost), Tile, 
								true, VehicleTex.TargeterMouseAttachment, closeWorldTabWhenFinished: false, onUpdate: null,
								extraLabelGetter: (GlobalTargetInfo target, List<FlightNode> path, float fuelCost) => vehicle.CompVehicleLauncher.launchProtocol.TargetingLabelGetter(target, Tile, path, fuelCost));
						}
					};
					if (!vehicle.CompVehicleLauncher.CanLaunchWithCargoCapacity(out string disableReason))
					{
						launchCommand.Disabled = true;
						launchCommand.disabledReason = disableReason;
					}
					yield return launchCommand;
				}
				if (vehiclePather.Moving)
				{
					yield return new Command_Toggle
					{
						defaultLabel = "CommandPauseCaravan".Translate(),
						defaultDesc = "CommandToggleCaravanPauseDesc".Translate(2f.ToString("0.#"), 0.3f.ToStringPercent()),
						icon = TexCommand.PauseCaravan,
						hotKey = KeyBindingDefOf.Misc1,
						isActive = () => vehiclePather.Paused,
						toggleAction = delegate()
						{
							if (!vehiclePather.Moving)
							{
								return;
							}
							vehiclePather.Paused = !vehiclePather.Paused;
						},
					};
				}
				else
				{
					if (VehiclesNeedRepairs)
					{
						yield return new Command_Toggle
						{
							defaultLabel = "VF_ToggleRepairVehicle".Translate(),
							defaultDesc = "VF_ToggleRepairVehicleDesc".Translate(),
							icon = VehicleTex.RepairVehicles,
							hotKey = KeyBindingDefOf.Misc2,
							isActive = () => !vehiclePather.Moving && repairing && VehiclesNeedRepairs,
							toggleAction = delegate ()
							{
								repairing = !repairing;
							},
						};
					}

					Command_Action disembark = new Command_Action();
					disembark.icon = VehicleTex.Anchor;
					disembark.defaultLabel = "VF_CommandDisembark".Translate(); //settlement != null ? "VF_CommandDockShip".Translate() : "VF_CommandDockShipDisembark".Translate();
					disembark.defaultDesc = "VF_CommandDisembarkDesc".Translate(); //settlement != null ? "VF_CommandDockShipDesc".Translate(settlement) : "VF_CommandDockShipObjectDesc".Translate();
					disembark.action = delegate ()
					{
						CaravanHelper.StashVehicles(this);
					};

					Settlement settlement = Find.WorldObjects.SettlementBaseAt(Tile);

					if (Find.World.Impassable(Tile))
					{
						disembark.Disable("VF_CommandDisembarkImpassableBiome".Translate());
					}
					if (settlement != null)
					{
						disembark.Disable("CommandSettleFailAlreadyHaveBase".Translate());
					}

					yield return disembark;
				}
				foreach (Gizmo gizmo2 in forage.GetGizmos())
				{
					yield return gizmo2;
				}
				foreach (WorldObject worldObject in Find.WorldObjects.ObjectsAt(Tile))
				{
					foreach (Gizmo gizmo3 in worldObject.GetCaravanGizmos(this))
					{
						yield return gizmo3;
					}
				}
			}

			foreach (VehiclePawn vehicle in VehiclesListForReading)
			{
				foreach (VehicleComp vehicleComp in vehicle.AllComps.Where(comp => comp is VehicleComp))
				{
					foreach (Gizmo gizmo in vehicleComp.CompCaravanGizmos())
					{
						yield return gizmo;
					}
				}
			}

			if (DebugSettings.ShowDevGizmos)
			{
				yield return new Command_Action
				{
					defaultLabel = "Vehicle Dev: Teleport to destination",
					action = delegate ()
					{
						Tile = vehiclePather.Destination;
						vehiclePather.StopDead();
					}
				};
				yield return new Command_Action
				{
					defaultLabel = "Repair all Vehicles",
					action = delegate ()
					{
						foreach (VehiclePawn vehicle in VehiclesListForReading)
						{
							vehicle.statHandler.components.ForEach(c => c.HealComponent(float.MaxValue));
						}
					}
				};
			}
		}

		public void Notify_VehicleTeleported()
		{
			vehicleTweener.ResetTweenedPosToRoot();
			vehiclePather.Notify_Teleported_Int();
		}

		public override void Notify_Merged(List<Caravan> group)
		{
			base.Notify_Merged(group);
			RecacheVehicles();
			RecacheStatAverages();
		}

		public override void Notify_MemberDied(Pawn member)
		{
			if (!Spawned)
			{
				Log.Error("Caravan member died in an unspawned caravan. Unspawned caravans shouldn't be kept for more than a single frame.");
			}
			if (!PawnsListForReading.NotNullAndAny(x => x is VehiclePawn vehicle && !vehicle.Dead && vehicle.AllPawnsAboard.NotNullAndAny((Pawn y) => y != member && IsOwner(y))))
			{
				RemovePawn(member);
				if (Faction == Faction.OfPlayer)
				{
					Find.LetterStack.ReceiveLetter("LetterLabelAllCaravanColonistsDied".Translate(), "LetterAllCaravanColonistsDied".Translate(Name).CapitalizeFirst(), LetterDefOf.NegativeEvent, new GlobalTargetInfo(Tile), null, null);
				}
				pawns.Clear();
				Destroy();
			}
			else
			{
				member.Strip();
				RemovePawn(member);
			}
			RecacheStatAverages();
		}

		public void RecacheVehicles()
		{
			vehicles = pawns.InnerListForReading.Where(pawn => pawn is VehiclePawn).Cast<VehiclePawn>().ToList();
			leadVehicle = null;
			ValidateCaravanType();
			RecacheStatAverages();
		}

		public void RecacheStatAverages()
		{
			float total = 0;
			int pawnCount = 0;
			foreach (Pawn pawn in PawnsListForReading)
			{
				if (pawn.IsColonistPlayerControlled || pawn.IsColonyMechPlayerControlled)
				{
					total += pawn.GetStatValue(StatDefOf.ConstructionSpeed);
					pawnCount++;
				}
			}
			float average = 1;
			if (pawnCount > 0)
			{
				average = total /pawnCount;
			}
			ConstructionAverage = average;

			VehiclesNeedRepairs = vehicles.Any(vehicle => vehicle.statHandler.ComponentsPrioritized.Any(c => c.HealthPercent < 1));
		}

		public virtual void PostInit()
		{
			initialized = true;
			RecacheVehicles();
			ValidateCaravanType();
		}

		/// <summary>
		/// Convert to vanilla caravan if no vehicles exist in VehicleCaravan
		/// </summary>
		private void ValidateCaravanType()
		{
			if (initialized && vehicles.NullOrEmpty())
			{
				Debug.Message($"VehicleCaravan {this} has no more vehicles. Converting to normal caravan. Pawns={string.Join($", ", pawns.InnerListForReading.Select(pawn => pawn.Label))}");
				List<Pawn> pawnsToTransfer = pawns.InnerListForReading.ToList();
				if (!pawnsToTransfer.NullOrEmpty())
				{
					RemoveAllPawns();
					Caravan caravan = CaravanMaker.MakeCaravan(pawnsToTransfer, Faction, Tile, true);
				}
				if (!Destroyed)
				{
					Destroy();
				}
			}
		}
		
		public override string GetInspectString()
		{
			StringBuilder stringBuilder = new StringBuilder();

			int colonists = 0;
			int animals = 0;
			int prisoners = 0;
			int downed = 0;
			int mentalState = 0;
			int vehicles = 0;

			vehicles++;
			foreach (Pawn pawn in PawnsListForReading)
			{
				if (pawn is VehiclePawn) vehicles++;
				if (pawn.IsColonist) colonists++;
				if (pawn.RaceProps.Animal) animals++;
				if (pawn.IsPrisoner) prisoners++;
				if (pawn.Downed) downed++;
				if (pawn.InMentalState) mentalState++;
			}

			if (vehicles >= 1)
			{
				vehicleCounts.Clear();
				{
					foreach (VehiclePawn vehicle in VehiclesListForReading)
					{
						if (vehicleCounts.ContainsKey(vehicle.VehicleDef))
						{
							vehicleCounts[vehicle.VehicleDef]++;
						}
						else
						{
							vehicleCounts[vehicle.VehicleDef] = 1;
						}
					}

					foreach ((VehicleDef def, int count) in vehicleCounts)
					{
						stringBuilder.Append($"{count} {def.LabelCap}, ");
					}
				}
				vehicleCounts.Clear();
			}
			stringBuilder.Append("CaravanColonistsCount".Translate(colonists, (colonists != 1) ? Faction.OfPlayer.def.pawnsPlural : Faction.OfPlayer.def.pawnSingular));
			if (animals == 1)
			{
				stringBuilder.Append(", " + "CaravanAnimal".Translate());
			}
			else if (animals > 1)
			{
				stringBuilder.Append(", " + "CaravanAnimalsCount".Translate(animals));
			}
			if (prisoners == 1)
			{
				stringBuilder.Append(", " + "CaravanPrisoner".Translate());
			}
			else if (prisoners > 1)
			{
				stringBuilder.Append(", " + "CaravanPrisonersCount".Translate(prisoners));
			}
			stringBuilder.AppendLine();
			if (mentalState > 0)
			{
				stringBuilder.Append("CaravanPawnsInMentalState".Translate(mentalState));
			}
			if (downed > 0)
			{
				if (mentalState > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append("CaravanPawnsDowned".Translate(downed));
			}
			foreach (VehiclePawn vehicle in VehiclesListForReading)
			{
				foreach (VehicleComp vehicleComp in vehicle.AllComps.Where(comp => comp is VehicleComp))
				{
					vehicleComp.CompCaravanInspectString(stringBuilder);
				}
			}
			if (mentalState > 0 || downed > 0)
			{
				stringBuilder.AppendLine();
			}

			if (vehiclePather.Moving)
			{
				if (vehiclePather.ArrivalAction != null)
				{
					stringBuilder.Append(vehiclePather.ArrivalAction.ReportString);
				}
				else if (this.HasBoat())
				{
					stringBuilder.Append("VF_Sailing".Translate());
				}
				else
				{
					stringBuilder.Append("CaravanTraveling".Translate());
				}
			}
			else
			{
				Settlement settlementBase = CaravanVisitUtility.SettlementVisitedNow(this);
				if (!(settlementBase is null))
				{
					stringBuilder.Append("CaravanVisiting".Translate(settlementBase.Label));
				}
				else
				{
					stringBuilder.Append("CaravanWaiting".Translate());
				}
			}
			if (vehiclePather.Moving)
			{
				float estimatedDaysToArrive = VehicleCaravanPathingHelper.EstimatedTicksToArrive(this, true) / 60000f;
				stringBuilder.AppendLine();
				stringBuilder.Append("CaravanEstimatedTimeToDestination".Translate(estimatedDaysToArrive.ToString("0.#")));
			}
			if (AllOwnersDowned)
			{
				stringBuilder.AppendLine();
				stringBuilder.Append("AllCaravanMembersDowned".Translate());
			}
			else if (AllOwnersHaveMentalBreak)
			{
				stringBuilder.AppendLine();
				stringBuilder.Append("AllCaravanMembersMentalBreak".Translate());
			}
			else if (ImmobilizedByMass)
			{
				stringBuilder.AppendLine();
				stringBuilder.Append("CaravanImmobilizedByMass".Translate());
			}
			if (needs.AnyPawnOutOfFood(out string text))
			{
				stringBuilder.AppendLine();
				stringBuilder.Append("CaravanOutOfFood".Translate());
				if (!text.NullOrEmpty())
				{
					stringBuilder.Append(" ");
					stringBuilder.Append(text);
					stringBuilder.Append(".");
				}
			}
			if (!vehiclePather.MovingNow)
			{
				int usedBedCount = beds.GetUsedBedCount();
				stringBuilder.AppendLine();
				stringBuilder.Append(CaravanBedUtility.AppendUsingBedsLabel("CaravanResting".Translate(), usedBedCount));
			}
			else
			{
				string inspectStringLine = carryTracker.GetInspectStringLine();
				if (!inspectStringLine.NullOrEmpty())
				{
					stringBuilder.AppendLine();
					stringBuilder.Append(inspectStringLine);
				}
				string inBedForMedicalReasonsInspectStringLine = beds.GetInBedForMedicalReasonsInspectStringLine();
				if (!inBedForMedicalReasonsInspectStringLine.NullOrEmpty())
				{
					stringBuilder.AppendLine();
					stringBuilder.Append(inBedForMedicalReasonsInspectStringLine);
				}
			}
			return stringBuilder.ToString();
		}

		public override void DrawExtraSelectionOverlays()
		{
			if (IsPlayerControlled && vehiclePather.curPath != null)
			{
				vehiclePather.curPath.DrawPath(this);
			}
			gotoMote.RenderMote();
		}

		public void TrySatisfyPawnsNeeds()
		{
			for (int i = pawns.Count - 1; i >= 0; i--)
			{
				Pawn pawn = pawns[i];
				if (pawn is VehiclePawn vehicle)
				{
					vehicle.TrySatisfyPawnNeeds();
				}
				else
				{
					VehiclePawn.TrySatisfyPawnNeeds(pawn);
				}
			}
		}

		public override void Tick()
		{
			base.Tick();
			vehiclePather.PatherTick();

			if (vehiclePather.MovingNow)
			{
				foreach (VehiclePawn vehicle in vehicles)
				{
					vehicle.CompFueledTravel?.ConsumeFuelWorld();
				}
			}
			else
			{
				int gameTicks = Find.TickManager.TicksGame;
				if (gameTicks % RepairMothballTicks == 0)
				{
					RecacheStatAverages();
				}
				if (VehiclesNeedRepairs && Repairing && gameTicks % RepairMothballTicks == 0)
				{
					RepairAllVehicles();
				}
			}
		}

		public void RepairAllVehicles()
		{
			LearnSkill(SkillDefOf.Construction, RepairMothballTicks);
			foreach (VehiclePawn vehicle in vehicles)
			{
				VehicleComponent component = vehicle.statHandler.ComponentsPrioritized.FirstOrDefault(c => c.HealthPercent < 1);
				if (component != null)
				{
					component.HealComponent(vehicle.GetStatValue(VehicleStatDefOf.RepairRate) * RepairMothballTicks / JobDriver_RepairVehicle.TicksForRepair);
					vehicle.CrashLanded = false;
					return; //Only repair 1 vehicle at a time
				}
			}
		}

		public void LearnSkill(SkillDef skillDef, int mothballTicks)
		{
			foreach (Pawn pawn in PawnsListForReading)
			{
				if (pawn.IsColonistPlayerControlled || pawn.IsColonyMechPlayerControlled)
				{
					pawn.skills?.Learn(skillDef, 0.08f * mothballTicks, false);
					pawn.records.Increment(RecordDefOf.ThingsRepaired);
				}
			}
		}

		public override void PostRemove()
		{
			base.PostRemove();
			vehiclePather.StopDead();
		}

		public override void SpawnSetup()
		{
			base.SpawnSetup();
			RecacheVehicles();
			vehicleTweener.ResetTweenedPosToRoot();
			
			//Necessary check for post load, otherwise registry will be null until spawned on map
			foreach (VehiclePawn vehicle in vehicles)
			{
				vehicle.RegisterEvents();
			}
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Deep.Look(ref vehiclePather, nameof(vehiclePather), new object[] { this });
			Scribe_Values.Look(ref repairing, nameof(repairing));

			if (Scribe.mode == LoadSaveMode.PostLoadInit)
			{
				initialized = true;
			}
		}
	}
}
