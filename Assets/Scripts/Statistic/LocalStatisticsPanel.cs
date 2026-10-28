using TMPro;
using UnityEngine;

/// <summary>Updates editor-authored local-statistics labels without creating UI.</summary>
public sealed class LocalStatisticsPanel : MonoBehaviour
{
    [SerializeField] TMP_Text playerNameText;
    [SerializeField] TMP_Text winsText;
    [SerializeField] TMP_Text lossesText;
    [SerializeField] TMP_Text winRateText;
    [SerializeField] GameObject emptyState;

    void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        int wins = LocalStatistics.Wins;
        int losses = LocalStatistics.Losses;
        int total = wins + losses;

        if (playerNameText != null)
            playerNameText.text = PlayerPrefs.GetString("Battleship.PlayerName", "Мой флот");
        if (winsText != null) winsText.text = wins.ToString();
        if (lossesText != null) lossesText.text = losses.ToString();
        if (winRateText != null)
            winRateText.text = total == 0 ? "—" : Mathf.RoundToInt(wins * 100f / total) + "%";
        if (emptyState != null) emptyState.SetActive(total == 0);
    }
}
