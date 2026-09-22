using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;

public class AuthBootstrap : MonoBehaviour
{

    [Header("API")]
    public string AuthUrl = "http://localhost:8000";

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
            UnityWebRequest.Get(AuthUrl);

        request.SetRequestHeader(
            "Authorization",
            "Bearer " + token
        );

        yield return request.SendWebRequest();

        // ✅ Токен валиден
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Token valid");
            SceneManager.LoadScene("Account");
        }
        else
        {
            Debug.Log("Token invalid: " + request.responseCode);

            // ❌ Токен протух / невалиден
            TokenStorage.Clear();
            SceneManager.LoadScene("Auth");
        }
    }
}
