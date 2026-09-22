using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class RatingPage : MonoBehaviour
{
    public RatingService ratingService;

    public PlayerRowUI rowPrefab;
    public Transform contentParent;

    void Start()
    {   
        StartCoroutine(Initialize());
    }

    IEnumerator Initialize()
    {
    string token = TokenStorage.Load();

    if (string.IsNullOrEmpty(token))
    {
        SceneManager.LoadScene("Auth");
        yield break;
    }

    yield return ratingService.GetTopPlayers(
        token,
        OnPlayersLoaded,
        OnError
    );
    }

    void OnPlayersLoaded(PlayerRating[] players)
    {
    for (int i = 0; i < players.Length; i++)
    {
        var row = Instantiate(rowPrefab, contentParent);

        row.Setup(players[i]);
        row.SetRank(i + 1);
    }
    }

    void OnError(string error)
    {
        Debug.LogError("Rating load error: " + error);
    }
}
