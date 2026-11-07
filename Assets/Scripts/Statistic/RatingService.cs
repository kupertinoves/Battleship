using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class RatingService : MonoBehaviour
{
    public IEnumerator GetTopPlayers(
        string jwtToken,
        System.Action<PlayerRating[]> onSuccess,
        System.Action<string> onError)
    {
        UnityWebRequest request = UnityWebRequest.Get(ServerConfig.RatingUrl);

        // JWT
        request.SetRequestHeader("Authorization", "Bearer " + jwtToken);
        request.SetRequestHeader("Content-Type", "application/json");

        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error);
            yield break;
        }

        // Unity не умеет массив сверху
        string wrappedJson =
            "{\"players\":" + request.downloadHandler.text + "}";

        PlayerRating[] players;
        try { players = JsonUtility.FromJson<PlayerRatingList>(wrappedJson).players; }
        catch (System.Exception e) { onError?.Invoke("bad response: " + e.Message); yield break; }

        // сервер отдаёт всех игроков без сортировки: берём топ-10 по победам
        if (players == null) players = new PlayerRating[0];
        System.Array.Sort(players, (a, b) =>
        {
            int c = b.victorious.CompareTo(a.victorious);
            return c != 0 ? c : a.defeats.CompareTo(b.defeats);
        });
        if (players.Length > 10) System.Array.Resize(ref players, 10);

        onSuccess?.Invoke(players);
    }
}
