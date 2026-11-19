using System;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using JetBrains.Annotations;
using RimWorldWaylith.Imgui;
using UnityEngine;
using Verse;
using Waylith;
using Waylith.Graphics;
using Waylith.Rendering;
using Waylith.Windowing;
using int2 = Brutal.Numerics.int2;

namespace SmashTools.Windowing;

[PublicAPI]
public sealed class WindowFrame : IDisposable
{
  private readonly Window window;
  private readonly WindowStack windowStack = new();
  private readonly WindowContext context;
  private readonly int2 size;

  private readonly EngineApplication app = EngineApplication.Instance;

  private IWindow platformWindow;
  private WindowHandler handler;
  private WindowRenderContext renderContext;
  private WindowGui gui;
  private WindowInput input;
  private FrameResult pendingRecovery = FrameResult.Success;

  private bool disposed;
  private bool closeRequested;

  public WindowFrame([NotNull] Window window, in WindowContext context)
  {
    ArgumentNullException.ThrowIfNull(window);

    if (!UnityData.IsInMainThread)
      throw new InvalidOperationException("Window must be owned by the main thread.");

    size = context.size ?? new int2((int)window.InitialSize.x, (int)window.InitialSize.y);
    if (size.X <= 0 || size.Y <= 0)
      throw new ArgumentOutOfRangeException(nameof(context), "Window dimensions must be larger than zero");

    window.doCloseButton = false;
    window.doCloseX = false;
    window.resizeable = false;
    window.draggable = false;
    window.drawShadow = false;

    this.window = window;
    this.context = context;
  }

  public void Run()
  {
    ObjectDisposedException.ThrowIf(disposed, this);

    if (!UnityData.IsInMainThread)
      throw new InvalidOperationException("Window must be opened on the main thread.");

    if (app?.WindowSystem is null || app.Headless || app.RenderContext == null)
      throw new InvalidOperationException("A graphical application is required.");

    handler = new WindowHandler(this);
    WindowOptions options = new()
    {
      Title = context.title,
      Size = WindowSize.Logical(size.X, size.Y),
      Kind = WindowKind.Toplevel,
      Visible = false,
      Decorated = context.drawTitleBar,
      Resizable = true
    };
    platformWindow = app.WindowSystem.CreateWindow(options, handler);
    app.BeforeGraphicsShutdown += OnGraphicsShutdown;
  }

  private void Show()
  {
    app.WaitForRenderThreadIdle();
    gui = new WindowGui(app, windowStack, renderContext);
    input = new WindowInput(platformWindow);

    // Calls pre- and post-open
    VkExtent2D extent = renderContext.GetExtent();
    using (new WindowStackContext(windowStack, extent.Width, extent.Height))
    {
      windowStack.Add(window);
      window.windowRect = new Rect(0, 0, extent.Width, extent.Height);
    }

    app.PrePollEvents += Update;
    platformWindow.Show();
  }

  private void CreateRenderContext(IWindow presentingWindow, SurfaceHandle surfaceHandle)
  {
    if (app.Headless || app.RenderContext == null || app.GraphicsDevice == null)
    {
      throw new InvalidOperationException("Cannot create ApplicationWindow in headless mode.");
    }

    VkSurfaceKHR surface = WindowSurfaces.Create(app.VulkanInstance, surfaceHandle);
    if (surface.IsNull())
    {
      throw new InvalidOperationException("Could not create window surface.");
    }
    try
    {
      if (!app.PhysicalDevice.GetSurfaceSupportKHR(app.GraphicsDevice.PresentFamilyIndex, surface))
      {
        throw new NotSupportedException(
          "The application's present queue cannot present to this window.");
      }

      PhysicalSize surfaceSize = presentingWindow.SurfaceSize.IsZero
        ? WindowSize.Logical(size.X, size.Y).ToPhysical(presentingWindow.ScaleFactor)
        : presentingWindow.SurfaceSize;
      app.GraphicsDevice.WaitIdleAndRun(() =>
      {
        renderContext = new WindowRenderContext(
          graphicsDevice: app.GraphicsDevice,
          presentingWindow: presentingWindow,
          surfaceHandle: surfaceHandle,
          surface: surface,
          surfaceSize: surfaceSize,
          colorFormat: app.RenderContext.ColorFormat,
          depthFormat: VkFormat.Undefined,
          requestedPresentMode: VkPresentModeKHR.FifoKHR,
          sampleCount: VkSampleCountFlags._1Bit,
          numFramesInFlight: 2);
      });
    }
    catch
    {
      app.VulkanInstance.DestroySurfaceKHR(surface, pAllocator: null);
      throw;
    }
  }

  private void Update(EngineApplication application)
  {
    if (!windowStack.IsOpen(window))
    {
      Dispose();
      return;
    }

    VkExtent2D extent;
    if (closeRequested)
    {
      bool removed;
      extent = renderContext.GetExtent();
      using (new WindowStackContext(windowStack, extent.Width, extent.Height))
      {
        removed = windowStack.TryRemove(window, doCloseSound: false);
      }
      if (!removed)
      {
        closeRequested = false;
      }
      else
      {
        Dispose();
      }
      return;
    }

    if (renderContext.IsSurfaceDetached && pendingRecovery != FrameResult.SurfaceLost)
      return;

    // Don't acquire swapchain images while minimized.
    if (renderContext.IsFramebufferMinimized())
      return;

    if (platformWindow.IsInResizeSession &&
        pendingRecovery != FrameResult.SurfaceLost &&
        (pendingRecovery == FrameResult.NeedsResize ||
         renderContext.HasFramebufferSizeMismatch()))
    {
      return;
    }

    if (pendingRecovery == FrameResult.SurfaceLost)
    {
      Rebuild(recreateSurface: true);
      pendingRecovery = FrameResult.Success;
    }
    else if (pendingRecovery == FrameResult.NeedsResize || renderContext.HasFramebufferSizeMismatch())
    {
      Rebuild(recreateSurface: false);
      pendingRecovery = FrameResult.Success;
    }

    extent = renderContext.GetExtent();
    window.windowRect = new Rect(0, 0, extent.Width, extent.Height);
    RenderFrame();
  }

  private void Rebuild(bool recreateSurface)
  {
    app.WaitForRenderThreadIdle();
    if (recreateSurface)
    {
      renderContext.RecreateSurfaceAndRebuild();
    }
    else
    {
      renderContext.Rebuild();
    }
  }

  private unsafe void RenderFrame()
  {
    // IMGUI's current context is global so we need to wait before acquiring
    app.WaitForRenderThreadIdle();
    (FrameResult result, AcquiredFrame acquiredFrame) = renderContext.TryAcquireNextFrame();

    if (result != FrameResult.Success)
    {
      RememberRecovery(result);
      return;
    }

    (FrameResources resources, CommandBuffer commands) = acquiredFrame;
    ImGuiDrawSnapshot snapshot = gui.Capture(input);
    commands.Reset();
    commands.Begin();
    {
      VkClearValue clear = new()
      {
        Color = new VkClearColorValue
        {
          Float32 = context.color
        }
      };
      VkRenderPassBeginInfo pass = new()
      {
        RenderPass = renderContext.GetRenderPass(),
        Framebuffer = resources.Framebuffer,
        RenderArea = new VkRect2D(renderContext.GetExtent()),
        ClearValueCount = 1,
        ClearValues = &clear
      };
      commands.BeginRenderPass(in pass, VkSubpassContents.Inline);
      {
        FrameContext frame = new(renderContext, resources, commands);
        gui.Record(snapshot, in frame);
      }
      commands.EndRenderPass();
    }
    commands.End();

    RememberRecovery(renderContext.TrySubmitFrame());
  }

  private void RememberRecovery(FrameResult result)
  {
    if (result is FrameResult.NeedsResize or FrameResult.SurfaceLost)
    {
      pendingRecovery = result;
    }
  }

  private void OnGraphicsShutdown(EngineApplication application)
  {
    Dispose();
  }

  private void CloseAllWindows()
  {
    using WindowStackContext wsc = new(windowStack);
    foreach (Window windowToRemove in windowStack.windows.ToArray())
    {
      windowStack.TryRemove(windowToRemove, doCloseSound: false);
    }
  }

  public void Dispose()
  {
    if (disposed)
      return;

    if (!UnityData.IsInMainThread)
      throw new InvalidOperationException("WindowFrame must be disposed on the main thread.");

    app.PrePollEvents -= Update;
    app.BeforeGraphicsShutdown -= OnGraphicsShutdown;

    app.WaitForRenderThreadIdle();

    CloseAllWindows();

    app.GraphicsDevice!.WaitIdleAndRun(() =>
    {
      gui?.Dispose();
      gui = null;
      renderContext?.Dispose();
      renderContext = null;
    });

    input?.Dispose();
    input = null;

    platformWindow?.Dispose();
    platformWindow = null;

    disposed = true;
  }

  private sealed class WindowHandler(WindowFrame parent) : IWindowHandler
  {
    void IWindowHandler.OnSurfaceCreated(IWindow window, SurfaceHandle surfaceHandle)
    {
      if (parent.renderContext is { IsSurfaceDetached: true } context)
      {
        context.AttachSurface(surfaceHandle);
        context.SetSurfaceSize(window.SurfaceSize);
        parent.pendingRecovery = FrameResult.SurfaceLost;
        return;
      }

      if (parent.renderContext != null)
        return;

      parent.CreateRenderContext(window, surfaceHandle);
      parent.Show();
    }

    void IWindowHandler.OnSurfaceDestroyed(IWindow window, SurfaceDestroyReason reason)
    {
      if (reason != SurfaceDestroyReason.Lost)
        return;

      WindowRenderContext context = parent.renderContext;
      if (context is null)
        return;

      parent.app.WaitForRenderThreadIdle();
      parent.app.GraphicsDevice!.WaitIdleAndRun(context.DetachSurface);
      parent.pendingRecovery = FrameResult.Success;
    }

    bool IWindowHandler.OnCloseRequested(IWindow window)
    {
      parent.closeRequested = true;
      return false;
    }

    void IWindowHandler.OnResized(IWindow window, in ResizeEvent e)
    {
      parent.renderContext?.SetSurfaceSize(e.SurfaceSize);
    }

    LogicalSize? IWindowHandler.OnScaleFactorChanged(IWindow window, double newScale, LogicalSize suggested)
    {
      return null;
    }

    void IWindowHandler.OnMoved(IWindow window, PhysicalPosition outerPosition)
    {
    }

    void IWindowHandler.OnStateChanged(IWindow window, WindowStates state)
    {
    }

    void IWindowHandler.OnStateRequestDenied(IWindow window, WindowStates requested)
    {
    }

    void IWindowHandler.OnFocusChanged(IWindow window, bool focused)
    {
      parent.input?.OnFocusChanged(focused);
    }

    void IWindowHandler.OnOcclusionChanged(IWindow window, bool occluded)
    {
    }

    void IWindowHandler.OnRedrawRequested(IWindow window)
    {
    }

    bool IWindowHandler.OnKeyDown(IWindow window, in KeyEvent e)
    {
      return false;
    }

    bool IWindowHandler.OnKeyUp(IWindow window, in KeyEvent e)
    {
      return false;
    }

    void IWindowHandler.OnModifiersChanged(IWindow window, Modifiers modifiers)
    {
    }

    void IWindowHandler.OnCompositionUpdated(IWindow window, in CompositionEvent e)
    {
    }

    void IWindowHandler.OnTextInput(IWindow window, in TextInputEvent e)
    {
    }

    void IWindowHandler.OnInputPaneChanged(IWindow window, in InputPaneEvent e)
    {
    }

    void IWindowHandler.OnPointerEntered(IWindow window, in PointerEvent e)
    {
      parent.input?.OnPointerEntered(in e);
    }

    void IWindowHandler.OnPointerExited(IWindow window, in PointerEvent e)
    {
      parent.input?.OnPointerExited(in e);
    }

    void IWindowHandler.OnPointerMoved(IWindow window, in PointerEvent e)
    {
      parent.input?.OnPointerMoved(in e);
    }

    void IWindowHandler.OnPointerPressed(IWindow window, in PointerEvent e)
    {
      parent.input?.OnPointerButton(in e, pressed: true);
    }

    void IWindowHandler.OnPointerReleased(IWindow window, in PointerEvent e)
    {
      parent.input?.OnPointerButton(in e, pressed: false);
    }

    void IWindowHandler.OnPointerCanceled(IWindow window, in PointerEvent e)
    {
    }

    void IWindowHandler.OnPointerCaptureLost(IWindow window, PointerId id)
    {
    }

    void IWindowHandler.OnScrolled(IWindow window, in ScrollEvent e)
    {
      parent.input?.OnScrolled(in e);
    }

    void IWindowHandler.OnGesture(IWindow window, in GestureEvent e)
    {
    }

    DragResponse IWindowHandler.OnDragEnter(IWindow window, in DragEvent e)
    {
      return DragResponse.Reject;
    }

    DragResponse IWindowHandler.OnDragOver(IWindow window, in DragEvent e)
    {
      return DragResponse.Reject;
    }

    void IWindowHandler.OnDragLeave(IWindow window)
    {
    }

    void IWindowHandler.OnDrop(IWindow window, in DropEvent e)
    {
    }

    void IWindowHandler.OnSafeAreaChanged(IWindow window, LogicalInsets insets)
    {
    }

    void IWindowHandler.OnResizeSessionChanged(IWindow window, bool active)
    {
      parent.renderContext?.SetResizeSession(active);
    }

    void IWindowHandler.OnFrameReady(IWindow window, in Waylith.Windowing.FrameTiming timing)
    {
    }

    void IWindowHandler.OnCurrentMonitorChanged(IWindow window, IMonitor monitor)
    {
    }

    void IWindowHandler.OnPopupDismissed(IWindow window)
    {
    }

    void IWindowHandler.OnPopupPositioned(IWindow window, LogicalRect relativeToParent)
    {
    }

    void IWindowHandler.OnPointerConstraintActiveChanged(IWindow window, bool active)
    {
    }
  }
}
