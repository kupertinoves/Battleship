using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class AuthBootstrap : MonoBehaviour
{

    public void OnSubmit()
    {
        StartCoroutine(CheckAuth());
    }

    IEnumerator CheckAuth()
    {
        string token = TokenStorage.Load();

        // ❌ Токена нет → логин
        if (string.IsNullOrEmpty(token))
        {
            SceneManager.LoadScene("Auth");
            yield break;
        }

        // ✅ Проверяем токен
        UnityWebRequest request =
            UnityWebRequest.Get(ServerConfig.CheckTokenUrl);

        request.SetRequestHeader(
            "Authorization",
            "Bearer " + token
        );

        request.timeout = 15;
        yield return request.SendWebRequest();

        // ✅ Токен валиден
        if (request.result == UnityWebRequest.Result.Success)
        {
            SceneManager.LoadScene("Account");
        }
        else
        {
            // 401 — токен невалиден; сетевые сбои токен не стирают
            if (request.responseCode == 401) TokenStorage.Clear();
            SceneManager.LoadScene("Auth");
        }
    }
}
