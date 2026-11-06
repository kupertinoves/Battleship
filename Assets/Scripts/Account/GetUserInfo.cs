using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using TMPro;


public class GetUserInfo : MonoBehaviour
{

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
            UnityWebRequest.Get(ServerConfig.UserInfoUrl);

        request.SetRequestHeader(
            "Authorization",
            "Bearer " + token
        );

        request.timeout = 15;
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var user = JsonUtility.FromJson<UserInfoResponse>(request.downloadHandler.text);

            Login.text = user.login;
            Email.text=user.email;
        }
        else if (request.responseCode == 401)
        {
            // токен протух / невалиден
            TokenStorage.Clear();
            SceneManager.LoadScene("Auth");
        }
        else
        {
            Login.text = "—";
            Email.text = "Нет соединения с сервером";
        }
    }
}
[System.Serializable]
public class UserInfoResponse
{
    public string login;
    public string email;
}
