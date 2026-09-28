using LudeonTK;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace SmashTools.Animations
{
	public class Dialog_MethodSelector : Dialog_ItemDropdown<MethodInfo>
	{
		private readonly IAnimator animator;
		private readonly AnimationEvent animationEvent;

		private static readonly List<MethodInfo> staticMethods = new List<MethodInfo>();

		public Dialog_MethodSelector(IAnimator animator, Rect rect, AnimationEvent animationEvent, Action<MethodInfo> onMethodPicked = null)
			: base(rect, EventMethods(animator), onMethodPicked, MethodName, itemTooltip: FullMethodSignature,
				  isSelected: (MethodInfo method) => animationEvent?.method != null && animationEvent.method.method == method)
		{
			this.animator = animator;
			this.animationEvent = animationEvent;
		}

		internal static string MethodName(MethodInfo method)
		{
			string typeName = GenTypes.GetTypeNameWithoutIgnoredNamespaces(method.DeclaringType);
			return $"{typeName}.{method.Name}";
		}

		internal static string FullMethodSignature(MethodInfo method)
		{
			string readout = MethodName(method);
			ParameterInfo[] parameters = method.GetParameters();
			if (!parameters.NullOrEmpty())
			{
				readout += $"( {string.Join(", ", parameters.Select(parameter => parameter.ParameterType.Name))} )";
			}
			return readout;
		}

		private static List<MethodInfo> EventMethods(IAnimator animator)
		{
			List<MethodInfo> list = [];
			AddEventMethods(animator, list);
			list.AddRange(staticMethods);
			return list;
		}

		private static void AddEventMethods(object obj, List<MethodInfo> methods)
		{
			foreach (MethodInfo method in obj.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (method.HasAttribute<AnimationEventAttribute>())
				{
					methods.Add(method);
				}
			}
		}

		internal static void InitStaticEventMethods()
		{
			if (staticMethods.Count > 0)
			{
				return;
			}
			foreach (Type type in GenTypes.AllTypes)
			{
				foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
				{
					if (methodInfo.TryGetAttribute(out AnimationEventAttribute _))
					{
						staticMethods.Add(methodInfo);
					}
				}
			}
		}
	}
}
