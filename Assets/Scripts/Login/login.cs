using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class LoginController : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField loginInput;
    public TMP_InputField passwordInput;
    public TMP_Text errorText;

    public void LoadNextScene()
    {
        Debug.Log("sceneName to load:MainMenu");
        SceneManager.LoadScene("MainMenu");
    }
    public void OnSubmit()
    {
        StartCoroutine(Login());
    }

    IEnumerator Login()
    {
        errorText.text = "";
        var form = new WWWForm();
        form.AddField("username", loginInput.text);
        form.AddField("password", passwordInput.text);

        using var request = UnityWebRequest.Post(ServerConfig.LoginUrl, form);
        request.timeout = 15;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            errorText.text = "Нет соединения с сервером";
            yield break;
        }

        if (request.responseCode == 401)
        {
            var err = JsonUtility.FromJson<LoginErrorResponse>(request.downloadHandler.text);
            errorText.text = string.IsNullOrEmpty(err?.detail) ? "Неверный логин или пароль" : err.detail;
            yield break;
        }

        if (request.responseCode >= 400)
        {
            errorText.text = "Ошибка сервера (" + request.responseCode + ")";
            Debug.LogError(request.downloadHandler.text);
            yield break;
        }
        if (request.responseCode==200){
            TokenStorage.Save(request.downloadHandler.text);
            LoadNextScene();
        }

        
    }
}

[System.Serializable]
public class LoginRequest
{
    public string username;
    public string password;
}

[System.Serializable]
public class LoginResponse
{
    public string access_token;
}
[System.Serializable]
public class LoginErrorResponse
{
    public string detail;
}