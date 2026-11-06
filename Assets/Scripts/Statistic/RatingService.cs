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

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error);
            yield break;
        }

        // Unity не умеет массив сверху
        string wrappedJson =
            "{\"players\":" + request.downloadHandler.text + "}";

        PlayerRatingList result =
            JsonUtility.FromJson<PlayerRatingList>(wrappedJson);

        onSuccess?.Invoke(result.players);
    }
}
