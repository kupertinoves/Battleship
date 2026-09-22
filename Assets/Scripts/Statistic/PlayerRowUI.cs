using TMPro;
using UnityEngine;

public class PlayerRowUI : MonoBehaviour
{
    public TMP_Text rankText;
    public TMP_Text loginText;
    public TMP_Text statsText;

    public void Setup(PlayerRating player)
    {
        loginText.text = player.login;

        int wins = Mathf.Max(0, player.victorious);
        int loses = Mathf.Max(0, player.defeats);

        statsText.text = $"W:{wins}  L:{loses}";
    }

    public void SetRank(int rank)
    {
        rankText.text = rank.ToString();
    }
}
