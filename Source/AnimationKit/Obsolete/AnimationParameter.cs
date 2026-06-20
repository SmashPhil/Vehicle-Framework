using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using SmashTools.Xml;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace SmashTools.Animations
{
	public class AnimationParameter : IXmlExport
	{
		private const float ContractedBy = 2;

		public AnimationParameterDef def;

		private float value;

		private string inputBuffer;

		public AnimationParameter(AnimationParameterDef def)
		{
			this.def = def;
		}

		public ushort Id => def.shortHash;

		public ParamType Type => def.type;

		public string Name => def.LabelCap;

		public float Value { get => value; internal set => this.value = value; }

		public void DrawInput(Rect rect)
		{
			switch (def.type)
			{
				case ParamType.Float:
					DrawFloatInput(rect);
					break;
				case ParamType.Int:
					DrawIntInput(rect);
					break;
				case ParamType.Bool:
					DrawBoolInput(rect);
					break;
				case ParamType.Trigger:
					DrawTriggerInput(rect);
					break;
				default:
					throw new NotImplementedException(nameof(ParamType));
			}
		}

		private void DrawFloatInput(Rect rect)
		{
			Widgets.TextFieldNumeric(rect, ref value, ref inputBuffer, min: float.MinValue, max: float.MaxValue);
		}

		private void DrawIntInput(Rect rect)
		{
			Widgets.TextFieldNumeric(rect, ref value, ref inputBuffer, min: int.MinValue, max: int.MaxValue);
		}

		private void DrawBoolInput(Rect rect)
		{
			bool checkOn = value != 0;
			Widgets.Checkbox(rect.position, ref checkOn, size: rect.height - ContractedBy * 2);
			value = checkOn ? 1 : 0;
		}

		private void DrawTriggerInput(Rect rect)
		{
			bool checkOn = value != 0;
			Texture2D buttonTex = checkOn ? Widgets.RadioButOnTex : UIData.RadioButOffTex;
			Rect buttonRect = new Rect(rect.x, rect.y, rect.height, rect.height).ContractedBy(ContractedBy);

			Color color = GUI.color;
			if (!GUI.enabled)
			{
				GUI.color = Color.gray;
			}
			GUI.DrawTexture(buttonRect, buttonTex);
			if (Widgets.ButtonInvisible(buttonRect))
			{
				checkOn = !checkOn;
				value = checkOn ? 1 : 0;
				SoundDefOf.Tick_Tiny.PlayOneShotOnCamera(null);
			}
			GUI.color = color;
		}

		void IXmlExport.Export()
		{
			XmlExporter.WriteObject(nameof(def), def);
			XmlExporter.WriteObject(nameof(value), value);
		}

		public enum ParamType
		{
			Float,
			Int,
			Bool,
			Trigger,
		}
	}
}
