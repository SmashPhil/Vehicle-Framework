using System;
using Brutal.GlfwApi;
using Brutal.VulkanApi;
using Brutal.VulkanApi.Abstractions;
using JetBrains.Annotations;
using RimWorldWaylith.Imgui;
using UnityEngine;
using Verse;
using Waylith;
using Waylith.Graphics;
using Waylith.Rendering;
using int2 = Brutal.Numerics.int2;

namespace SmashTools.Rendering;

[PublicAPI]
public sealed class WindowFrame : IDisposable
{
  private readonly Window window;
  private readonly WindowStack windowStack = new();
  private readonly WindowContext context;
  private readonly int2 size;

  private readonly EngineApplication app = EngineApplication.Instance;

  private GlfwWindow glfwWindow;
  private WindowRenderContext renderContext;
  private WindowGui gui;
  private WindowInput input;
  private FrameResult pendingRecovery = FrameResult.Success;

  private bool disposed;

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

    if (glfwWindow != null)
      return;

    if (app == null || app.Headless || app.RenderContext == null)
      throw new InvalidOperationException("A graphical application is required.");

    Glfw.WindowHint(GlfwWindowHint.ClientApi, 0);
    Glfw.WindowHint(GlfwWindowHint.Visible, 0);
    Glfw.WindowHint(GlfwWindowHint.Decorated, context.drawTitleBar ? 1 : 0);
    Glfw.WindowHint(GlfwWindowHint.Resizable, 1);
    Glfw.WindowHint(GlfwWindowHint.Floating, 0);
    Glfw.WindowHint(GlfwWindowHint.Maximized, 0);
    Glfw.WindowHint(GlfwWindowHint.FocusOnShow, 1);
    glfwWindow = Glfw.CreateWindow(new GlfwWindow.CreateInfo
    {
      Title = context.title,
      Size = size
    });

    CreateRenderContext();
    app.WaitForRenderThreadIdle();
    gui = new WindowGui(app, windowStack, renderContext);
    input = new WindowInput(glfwWindow);

    // Calls pre- and post-open
    VkExtent2D extent = renderContext.GetExtent();
    using (new WindowStackContext(windowStack, extent.Width, extent.Height))
    {
      windowStack.Add(window);
      window.windowRect = new Rect(0, 0, extent.Width, extent.Height);
    }

    app.PrePollEvents += Update;
    app.BeforeGraphicsShutdown += OnGraphicsShutdown;
    glfwWindow.Visible = true;
  }

  private void CreateRenderContext()
  {
    int result = Glfw.CreateWindowSurface(app.VulkanInstance.VkHandle, glfwWindow, out ulong handle);

    if (result != 0 || handle == 0)
    {
      throw new InvalidOperationException(
        $"Could not create window surface: Vulkan result {result}.");
    }

    if (app.Headless || app.RenderContext == null || app.GraphicsDevice == null)
    {
      throw new InvalidOperationException("Cannot create ApplicationWindow in headless mode.");
    }

    var surface = new VkSurfaceKHR((nint)handle);
    try
    {
      if (!app.PhysicalDevice.GetSurfaceSupportKHR(app.GraphicsDevice.PresentFamilyIndex, surface))
      {
        throw new NotSupportedException(
          "The application's present queue cannot present to this window.");
      }

      app.GraphicsDevice.WaitIdleAndRun(() =>
      {
        renderContext = new WindowRenderContext(
          graphicsDevice: app.GraphicsDevice,
          window: glfwWindow,
          surface: surface,
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
    if (glfwWindow.ShouldClose)
    {
      bool removed;
      extent = renderContext.GetExtent();
      using (new WindowStackContext(windowStack, extent.Width, extent.Height))
      {
        removed = windowStack.TryRemove(window, doCloseSound: false);
      }
      if (!removed)
      {
        glfwWindow.ShouldClose = false;
      }
      else
      {
        Dispose();
      }
      return;
    }

    // Don't acquire swapchain images while minimized.
    if (renderContext.IsFramebufferMinimized())
      return;

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
    app.GraphicsDevice!.WaitIdleAndRun(() =>
    {
      gui?.Dispose();
      gui = null;
      if (recreateSurface)
      {
        renderContext.RecreateSurfaceAndRebuild();
      }
      else
      {
        renderContext.Rebuild();
      }
      gui = new WindowGui(app, windowStack, renderContext);
    });
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
    VkExtent2D extent = renderContext.GetExtent();
    using WindowStackContext wsc = new(windowStack, extent.Width, extent.Height);
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
      throw new InvalidOperationException("ApplicationWindow must be disposed on the main thread.");

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

    glfwWindow?.Dispose();
    glfwWindow = null;

    disposed = true;
  }
}
