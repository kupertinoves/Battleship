using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using TMPro;


public class GetUserInfo : MonoBehaviour
{

    [Header("API")]
    public string AuthUrl = "http://localhost:8000";

    [Header("Login")]
    public TMP_Text Login;

    [Header("Email")]
    public TMP_Text Email;

    public void Start()
    {
        StartCoroutine(GetInfo());
    }

    IEnumerator GetInfo()
    {
        Login.text="";
        Email.text="";
        string token = TokenStorage.Load();
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

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Token valid");
            Debug.Log(request.downloadHandler.text);
            var user = JsonUtility.FromJson<UserInfoResponse>(request.downloadHandler.text);

            Login.text = user.login;
            Email.text=user.email;
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
[System.Serializable]
public class UserInfoResponse
{
    public string login;
    public string email;
}
