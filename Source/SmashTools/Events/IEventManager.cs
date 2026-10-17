namespace SmashTools;

public interface IEventManager<T>
{
  EventManager<T> EventRegistry { get; set; }
}