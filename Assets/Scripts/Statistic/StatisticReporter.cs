using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Отправляет результат онлайн-матча на сервис статистики (POST /statistic/add).</summary>
public static class StatisticReporter
{
    [System.Serializable]
    class AddRequest { public string login; public int defeats; public int victorious; }

    sealed class Runner : MonoBehaviour { }
    static Runner runner;

    /// <summary>Запрос выполняется на объекте, переживающем смену сцены.</summary>
    public static void Report(bool win)
    {
        string token = TokenStorage.Load();
        if (string.IsNullOrEmpty(token)) return;

        if (runner == null)
        {
            var go = new GameObject("StatisticReporter");
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<Runner>();
        }
        runner.StartCoroutine(Send(token, win));
    }

    static IEnumerator Send(string token, bool win)
    {
        // сервер прибавляет значения к статистике владельца токена
        var body = new AddRequest { login = "", defeats = win ? 0 : 1, victorious = win ? 1 : 0 };
        var request = new UnityWebRequest(ServerConfig.StatisticAddUrl, "POST")
        {
            uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body))),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = 15
        };
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + token);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
            Debug.LogWarning("Statistic add failed: " + request.error + " " + request.downloadHandler.text);
        request.Dispose();
    }
}
