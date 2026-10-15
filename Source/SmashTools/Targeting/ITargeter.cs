namespace SmashTools.Targeting;

public interface ITargeter
{
  void OnStart();
  void OnStop();
  void Update();
  void OnGUI();
}