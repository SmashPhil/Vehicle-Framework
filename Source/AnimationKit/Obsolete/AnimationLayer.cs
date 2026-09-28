using SmashTools.Xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using StateType = SmashTools.Animations.AnimationState.StateType;

namespace SmashTools.Animations
{
	public class AnimationLayer : IXmlExport
	{
		public string name;
		
		public List<AnimationState> states = new List<AnimationState>();

		/// <summary>
		/// For Xml Deserialization
		/// </summary>
		public AnimationLayer()
		{
		}

		public AnimationController Controller { get; internal set; }

		public AnimationState AddState(string name, IntVec2 position, StateType type = StateType.None)
		{
			if (type != StateType.None && states.Any(state => state.Type == type))
			{
				Log.Error($"Attempting to load duplicate special state type to AnimationLayer {name}.");
				return null;
			}
			if (type == StateType.None && !states.Any(state => state.Type == StateType.Default))
			{
				type = StateType.Default;
			}
			AnimationState state = new AnimationState(name, type);
			state.position = position;
			if (type == StateType.Default)
			{
				AnimationState entryState = states.First(state => state.Type == StateType.Entry);
				entryState.AddTransition(state);
			}
			state.Layer = this;
			states.Add(state);
			return state;
		}

		public void RemoveState(string name)
		{
			for (int i = 0; i < states.Count; i++)
			{
				if (states[i].name == name)
				{
					states.RemoveAt(i);
					return;
				}
			}
		}

		private void ResolveConnections()
		{
			if (states.NullOrEmpty()) return;

			Dictionary<Guid, AnimationState> guidLookup  = states.ToDictionary(state => state.guid);
			foreach (AnimationState state in states)
			{
				foreach (AnimationTransition transition in state.transitions)
				{
					transition.FromState = state;
					if (guidLookup.TryGetValue(transition.toStateGuid, out AnimationState toState))
					{
						transition.ToState = toState;
					}
				}
				// Cleanup to avoid broken transitions. Will need intervention by modder
				int count = state.transitions.RemoveAll(transition => transition.ToState == null);
				if (count > 0) Log.Error($"{count} invalid transitions purged! This may break some animation states.");
			}
		}

		internal void ResolveReferences()
		{
			ResolveConnections();
			foreach (AnimationState state in states)
			{
				state.Layer = this;
				state.ResolveReferences();
			}
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteObject(nameof(name), name);
			XmlExporter.WriteCollection(nameof(states), states);
		}

		public static AnimationLayer CreateLayer(string name)
		{
			AnimationLayer layer = new AnimationLayer();
			layer.name = name;

			int exitX = AnimationControllerEditor.StateWidth * 2;
			int entryX = exitX + AnimationControllerEditor.StateWidth;

			layer.AddState("Entry", new IntVec2(-entryX, 0), type: StateType.Entry);
			//layer.AddState("Any State", new IntVec2(-10, 5), type: StateType.AnyState);
			layer.AddState("Exit", new IntVec2(exitX, 0), type: StateType.Exit);

			return layer;
		}
	}
}
