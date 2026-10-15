using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Verse;

namespace SmashTools
{
	public static class AnimationTargetHandler
	{
		public static List<AnimatorObject> GetAnimators(this IAnimationTarget animationTarget,
			StringBuilder stringBuilder = null)
		{
			List<AnimatorObject> animators = new List<AnimatorObject>();
			foreach (ThingComp thingComp in animationTarget.Thing.AllComps)
			{
				stringBuilder?.AppendLine($"Starting Traversal on {animationTarget}.{thingComp.GetType()}");
				foreach (AnimatorObject animatorObject in GetAnimatorRecursive(thingComp, string.Empty, string.Empty,
					stringBuilder))
				{
					animators.Add(animatorObject);
				}
			}
			return animators;
		}

		private static IEnumerable<AnimatorObject> GetAnimatorRecursive(object parent, string category, string prefix,
			StringBuilder stringBuilder = null)
		{
			//If parent GraphEditable is null, skip.  Should never reach here if type is LinearCurve, should instantiate new object below
			if (parent == null)
			{
				yield break;
			}
			FieldInfo[] fieldInfos =
				parent.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			foreach (FieldInfo fieldInfo in fieldInfos)
			{
				string fieldCategory = category;
				if (fieldInfo.TryGetAttribute(out GraphEditableAttribute graphEditableAttribute))
				{
					if (fieldInfo.FieldType.IsClass)
					{
						if (!graphEditableAttribute.Category.NullOrEmpty())
						{
							if (fieldCategory.NullOrEmpty())
							{
								fieldCategory = graphEditableAttribute.Category;
							}
							else
							{
								fieldCategory += $".{graphEditableAttribute.Category}";
							}
							stringBuilder?.AppendLine($"Starting Category: {fieldCategory}");
						}
						stringBuilder?.AppendLine(
							$"Processing <method>{fieldInfo.DeclaringType}.{fieldInfo.Name}</method> (Type=<type>{fieldInfo.FieldType}</type>)");
						object fieldObj = fieldInfo.GetValue(parent);
						if (fieldObj == null && fieldInfo.FieldType.SameOrSubclassOf(typeof(LinearCurve)))
						{
							fieldObj = Activator.CreateInstance(fieldInfo.FieldType);
							fieldInfo.SetValue(parent, fieldObj);
						}
						if (fieldObj is LinearCurve)
						{
							if (fieldCategory.NullOrEmpty())
							{
								Log.Error(
									$"Attempting to add {fieldInfo.Name} to GraphEditor cache with no category.  Must assign category name to either containing objects or the field itself.");
								continue;
							}
							stringBuilder?.AppendLine($"Adding {fieldObj} to category=\"{fieldCategory}\"");
							yield return new AnimatorObject(parent, fieldInfo, fieldCategory, prefix);
						}
						else
						{
							foreach (AnimatorObject animatorObject in GetAnimatorRecursive(fieldObj, fieldCategory,
								graphEditableAttribute.Prefix, stringBuilder))
							{
								yield return animatorObject;
							}
						}
					}
					else
					{
						Log.Error(
							$"Attempting to add {fieldInfo.DeclaringType}.{fieldInfo.Name} to Graph Editor. Field must be a reference type for editing to work. Skipping...");
					}
				}
			}
		}
	}
}