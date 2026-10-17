using System;
using HarmonyLib;
using RimWorld;
using SmashTools.Patching;
using SmashTools.Rendering;
using UnityEngine;
using Verse;
using static SmashTools.Dialog_GraphEditor;
using Object = UnityEngine.Object;

namespace SmashTools
{
  [StaticConstructorOnStartup]
  public static class CameraView
  {
    private const float MinZoom = 2;
    private const float MaxZoom = 60;
    private const float DefaultCameraSize = 8;
    private const float PageKeyZoomRate = 4;
    private const float ZoomScaleFromAltDenominator = 35;
    private const float CameraViewerZoomRate = 0.55f;

    private static Camera camera;
    private static RenderTexture renderTexture;
    private static float orthographicSize;
    private static CameraMapConfig cameraConfig = new CameraMapConfig_Normal();
    public static AnimationSettings animationSettings = new AnimationSettings();

    private static int lastViewRectGetFrame = -1;
    private static CellRect lastViewRect;
    private static float desiredSize = 24f;

    private static Vector3 rootPos;
    private static float rootSize;

    public static readonly Texture2D pauseTexture =
      ContentFinder<Texture2D>.Get("SmashTools/VideoPause");

    public static readonly Texture2D playTexture =
      ContentFinder<Texture2D>.Get("SmashTools/VideoPlay");

    public static readonly Texture2D dragHandleIcon =
      ContentFinder<Texture2D>.Get("UI/Icons/LifeStage/Adult", true);


    public static Vector3 RootPos
    {
      get => rootPos;
      set => rootPos = value;
    }

    public static float OrthographicSize
    {
      get => orthographicSize;
      set => orthographicSize = value;
    }

    public static bool InUse { get; private set; }

    private static bool Patched { get; set; }

    private static bool LockedToMainCamera { get; set; } = false;

    public static CellRect CurrentViewRect
    {
      get
      {
        if (Time.frameCount != lastViewRectGetFrame)
        {
          lastViewRect = default;
          float num = UI.screenWidth / (float)UI.screenHeight;
          Vector3 currentRealPosition = camera.transform.position;
          lastViewRect.minX = Mathf.FloorToInt(currentRealPosition.x - rootSize * num - 1f);
          lastViewRect.maxX = Mathf.CeilToInt(currentRealPosition.x + rootSize * num);
          lastViewRect.minZ = Mathf.FloorToInt(currentRealPosition.z - rootSize - 1f);
          lastViewRect.maxZ = Mathf.CeilToInt(currentRealPosition.z + rootSize);
          lastViewRectGetFrame = Time.frameCount;
        }
        return lastViewRect;
      }
    }

    public static void HandleZoom()
    {
      float zoom = 0;
      if (Event.current.type == EventType.ScrollWheel)
      {
        zoom -= Event.current.delta.y * CameraViewerZoomRate;
        Event.current.Use();
      }
      if (KeyBindingDefOf.MapZoom_In.KeyDownEvent)
      {
        zoom += PageKeyZoomRate;
        Event.current.Use();
      }
      if (KeyBindingDefOf.MapZoom_Out.KeyDownEvent)
      {
        zoom -= PageKeyZoomRate;
        Event.current.Use();
      }
      desiredSize -= zoom * cameraConfig.zoomSpeed * rootSize / ZoomScaleFromAltDenominator;
      desiredSize = Mathf.Clamp(desiredSize, MinZoom, MaxZoom);
    }

    public static void Update(Vector3 position)
    {
      if (InUse)
      {
        rootPos = new Vector3(position.x,
          15f + (rootSize - cameraConfig.sizeRange.min) /
          (cameraConfig.sizeRange.max - cameraConfig.sizeRange.min) * 50f, position.z);

        OrthographicSize = Mathf.Lerp(OrthographicSize, desiredSize, 0.05f);

        camera.orthographicSize = OrthographicSize;
        camera.transform.position = rootPos;

        if (LockedToMainCamera)
        {
          Find.Camera.transform.position = camera.transform.position;
          Find.Camera.orthographicSize = OrthographicSize;
        }
      }
    }

    public static bool RenderAt(Rect rect)
    {
      if (!InUse)
      {
        return false;
      }
      try
      {
        GUI.DrawTexture(rect, renderTexture);
        if (Mouse.IsOver(rect))
        {
          HandleZoom();
        }
      }
      catch (Exception ex)
      {
        Log.ErrorOnce($"Exception thrown in CameraView. Exception = {ex}",
          "CameraView_RT".GetHashCode());
        return false;
      }
      return true;
    }

    public static void DrawMapGridInView()
    {
      foreach (IntVec3 cell in CurrentViewRect)
      {
        GenDraw.DrawLineBetween(cell.ToVector3(), cell.ToVector3() + new Vector3(1, 0, 0));
        GenDraw.DrawLineBetween(cell.ToVector3(), cell.ToVector3() + new Vector3(0, 0, 1));
      }
    }

    public static void Close()
    {
      InUse = false;
      if (camera && camera.gameObject)
      {
        renderTexture.Release();
        Object.Destroy(renderTexture);
        Object.Destroy(camera.gameObject);

        camera = null;
        renderTexture = null;
      }
    }

    public static void ResetSize()
    {
      desiredSize = DefaultCameraSize;
      rootSize = desiredSize;
    }

    public static void Start(float orthographicSize = 11, CameraMapConfig cameraConfig = null)
    {
      Start(new IntVec2(512, 512), orthographicSize: orthographicSize, cameraConfig: cameraConfig);
    }

    public static void Start(IntVec2 size, float orthographicSize = 11,
      RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGBFloat,
      CameraMapConfig cameraConfig = null)
    {
      InUse = true;
      try
      {
        if (cameraConfig == null)
        {
          cameraConfig = (CameraMapConfig)Activator.CreateInstance(typeof(CameraMapConfig_Normal));
        }
        CameraView.cameraConfig = cameraConfig;
        CameraView.camera = CreateCamera(orthographicSize);
        CameraView.renderTexture = CreateRenderTexture(size, renderTextureFormat);
        CameraView.camera.targetTexture = renderTexture;

        CameraView.camera.transform.position = Find.Camera.transform.position;
        CameraView.camera.transform.rotation = Find.Camera.transform.rotation;

        ResetSize();
        PatchOcclusionCulling();
      }
      catch
      {
        Close();
        throw;
      }
    }

    internal static Camera CreateCamera(float orthographicSize)
    {
      GameObject cameraObject = new GameObject("CameraView_GameObject");
      cameraObject.SetActive(true);

      CameraView.orthographicSize = orthographicSize;

      Camera camera = cameraObject.AddComponent<Camera>();
      camera.orthographic = true;
      camera.cameraType = CameraType.Game;
      camera.orthographicSize = orthographicSize;
      return camera;
    }

    internal static RenderTexture CreateRenderTexture(IntVec2 size,
      RenderTextureFormat renderTextureFormat)
    {
      RenderTexture renderTexture =
        new RenderTexture(size.x, size.z, 16,
          renderTextureFormat.OrNextSupportedFormat());
      renderTexture.name = "CameraView_RT";
      renderTexture.Create();
      return renderTexture;
    }

    private static void PatchOcclusionCulling()
    {
      if (!Patched)
      {
        Patched = true;
        try
        {
          HarmonyPatcher.Harmony.Patch(
            original: AccessTools.PropertyGetter(typeof(MapDrawer), "ViewRect"),
            postfix: new HarmonyMethod(typeof(CameraView),
              nameof(CameraPreviewViewRect)));
        }
        catch (Exception ex)
        {
          LockedToMainCamera = true;
          Log.Error(
            $"Failed to patch CameraView rect for occlusion culling. Animations will not work outside main camera rect, locking main camera to viewer.\nException={ex}");
        }
      }
    }

    private static void CameraPreviewViewRect(ref CellRect __result, Map ___map)
    {
      if (InUse)
      {
        __result =
          new CellRect(0, 0, ___map.Size.x,
            ___map.Size
             .z); //Render the entire map for animations to avoid having to choose between culling either camera
      }
    }
  }
}