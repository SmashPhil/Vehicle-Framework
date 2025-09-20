using UnityEngine;
using Verse;

namespace Vehicles;

public class ColorPicker
{
	private static readonly Color Blackist = new(0.06f, 0.06f, 0.06f);

	private bool draggingColorPicker;
	private bool draggingHue;

	private readonly Texture2D colorChart = new(255, 255);
	private readonly Texture2D hueChart = new(1, 255);

	public delegate void SetColor(float h, float s, float v);

	public ColorPicker()
	{
		for (int i = 0; i < 255; i++)
		{
			hueChart.SetPixel(0, i, Color.HSVToRGB(Mathf.InverseLerp(0f, 255f, i), 1f, 1f));
		}
		hueChart.Apply(false);
		for (int j = 0; j < 255; j++)
		{
			for (int k = 0; k < 255; k++)
			{
				Color color = Color.clear;
				Color c = Color.Lerp(color, Color.white, Mathf.InverseLerp(0f, 255f, j));
				color = Color32.Lerp(Color.black, c, Mathf.InverseLerp(0f, 255f, k));
				colorChart.SetPixel(j, k, color);
			}
		}
		colorChart.Apply(false);
	}

	/// <summary>
	/// Draw ColorPicker and HuePicker
	/// </summary>
	public Rect Draw(Rect fullRect, ref float hue, ref float saturation,
		ref float value, SetColor setColor)
	{
		Rect rect = fullRect.ContractedBy(10f);
		rect.width = 15f;
		if (Input.GetMouseButtonDown(0) && Mouse.IsOver(rect) && !draggingHue)
		{
			draggingHue = true;
		}
		if (draggingHue && Event.current.isMouse)
		{
			float num = hue;
			hue = Mathf.InverseLerp(rect.height, 0f, Event.current.mousePosition.y - rect.y);
			if (!Mathf.Approximately(hue, num))
			{
				setColor(hue, saturation, value);
			}
		}
		if (Input.GetMouseButtonUp(0))
		{
			draggingHue = false;
		}
		Widgets.DrawBoxSolid(rect.ExpandedBy(1f), Color.grey);
		Widgets.DrawTexturePart(rect, new Rect(0f, 0f, 1f, 1f), hueChart);
		Rect rect2 = new(0f, 0f, 16f, 16f)
		{
			center = new Vector2(rect.center.x, rect.height * (1f - hue) + rect.y).Rounded()
		};

		Widgets.DrawTextureRotated(rect2, VehicleTex.ColorHue, 0f);
		rect = fullRect.ContractedBy(10f);
		rect.x = rect.xMax - rect.height;
		rect.width = rect.height;
		if (Input.GetMouseButtonDown(0) && Mouse.IsOver(rect) && !draggingColorPicker)
		{
			draggingColorPicker = true;
		}
		if (draggingColorPicker)
		{
			saturation = Mathf.InverseLerp(0f, rect.width, Event.current.mousePosition.x - rect.x);
			value = Mathf.InverseLerp(rect.width, 0f, Event.current.mousePosition.y - rect.y);
			setColor(hue, saturation, value);
		}
		if (Input.GetMouseButtonUp(0))
		{
			draggingColorPicker = false;
		}
		Widgets.DrawBoxSolid(rect.ExpandedBy(1f), Color.grey);
		Widgets.DrawBoxSolid(rect, Color.white);
		GUI.color = Color.HSVToRGB(hue, 1f, 1f);
		Widgets.DrawTextureFitted(rect, colorChart, 1f);
		GUI.color = Color.white;
		GUI.BeginClip(rect);
		rect2.center = new Vector2(rect.width * saturation, rect.width * (1f - value));
		if (value >= 0.4f && (hue <= 0.5f || saturation <= 0.5f))
		{
			GUI.color = Blackist;
		}
		Widgets.DrawTextureFitted(rect2, VehicleTex.ColorPicker, 1f);
		GUI.color = Color.white;
		GUI.EndClip();
		return rect;
	}
}