using System.IO;
using Verse;

namespace Vehicles;

internal class SaveTester
{
  private const string SaveFileName = "SaveTester_TEMP";

  public static void Clear()
  {
    File.Delete(GenFilePaths.FilePathForSavedGame(SaveFileName));
  }

  public static void Write()
  {
    TestActions.ClearVanillaVehiclesExpandedTrackerCache();
    GameDataSaveLoader.SaveGame(SaveFileName);
  }

  public static void WriteAndLoad()
  {
    Write();
    GameDataSaveLoader.LoadGame(SaveFileName);
  }
}