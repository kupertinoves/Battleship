using UnityEngine;

/// <summary>Offline match totals for this installation of the game.</summary>
public static class LocalStatistics
{
    const string WinsKey = "Battleship.Stats.Wins";
    const string LossesKey = "Battleship.Stats.Losses";

    public static int Wins => PlayerPrefs.GetInt(WinsKey, 0);
    public static int Losses => PlayerPrefs.GetInt(LossesKey, 0);

    public static void RecordWin()
    {
        PlayerPrefs.SetInt(WinsKey, Wins + 1);
        PlayerPrefs.Save();
    }

    public static void RecordLoss()
    {
        PlayerPrefs.SetInt(LossesKey, Losses + 1);
        PlayerPrefs.Save();
    }
}
