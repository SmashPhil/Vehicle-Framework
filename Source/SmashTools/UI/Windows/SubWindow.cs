using Verse;

namespace SmashTools;

public abstract class SubWindow : Window, IWindowEventListener
{
	private readonly Window parent;

	protected SubWindow(Window parent)
	{
		this.parent = parent;
	}

	public void RegisterEvents()
	{
		this.Register(parent, OnParentClosed, WindowEvents.Event.Closed);
	}

	public void DeregisterEvents()
	{
		this.Deregister();
	}

	private void OnParentClosed()
	{
		Close();
	}

	public override void PostClose()
	{
		base.PostClose();
		DeregisterEvents();
	}
}