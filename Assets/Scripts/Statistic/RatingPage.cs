using System.Collections;
using TMPro;
using UnityEngine;

public class RatingPage : MonoBehaviour
{
    [SerializeField] RatingService ratingService;
    [SerializeField] PlayerRowUI rowPrefab;
    [SerializeField] Transform contentParent;
    [SerializeField] TMP_Text ratingStatusText;

    void Start()
    {
        StartCoroutine(LoadRating());
    }

    IEnumerator LoadRating()
    {
        if (ratingService == null || rowPrefab == null || contentParent == null || ratingStatusText == null)
        {
            Debug.LogError("RatingPage is missing one or more Inspector references.", this);
            ShowStatus("Онлайн-рейтинг сейчас недоступен.");
            yield break;
        }

        string token = TokenStorage.Load();
        if (string.IsNullOrEmpty(token))
        {
            ShowStatus("Войдите, чтобы видеть онлайн-рейтинг.\nЛокальная статистика доступна выше.");
            yield break;
        }

        ShowStatus("Загрузка рейтинга игроков…");
        yield return ratingService.GetTopPlayers(token, OnPlayersLoaded, OnError);
    }

    void OnPlayersLoaded(PlayerRating[] players)
    {
        if (players == null || players.Length == 0)
        {
            ShowStatus("В рейтинге пока нет игроков.");
            return;
        }

        for (int i = 0; i < players.Length; i++)
        {
            PlayerRowUI row = Instantiate(rowPrefab, contentParent);
            row.Setup(players[i]);
            row.SetRank(i + 1);
        }

        ratingStatusText.gameObject.SetActive(false);
    }

    void OnError(string error)
    {
        Debug.LogWarning("Rating load failed: " + error, this);
        ShowStatus("Не удалось загрузить рейтинг. Проверьте подключение и попробуйте позже.");
    }

    void ShowStatus(string message)
    {
        if (ratingStatusText == null) return;
        ratingStatusText.text = message;
        ratingStatusText.gameObject.SetActive(true);
    }
}
