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
    public string authUrl = "http://localhost:8000/register";
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
        var data = new LoginRequest
        {
            username = loginInput.text,
            password = passwordInput.text,
            //email = emailInput != null ? emailInput.text : null
        };
        Debug.Log("login: " + loginInput.text);
        Debug.Log("password: " + passwordInput.text);

        string body = "username="+loginInput.text+"&password="+passwordInput.text;
        Debug.Log(body);

        var request = new UnityWebRequest(authUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(body);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

        yield return request.SendWebRequest();
        Debug.Log(request.responseCode);

        if (request.responseCode == 401)
        {
            var err = JsonUtility.FromJson<LoginErrorResponse>(request.downloadHandler.text);
            Debug.Log(request.downloadHandler.text);
            errorText.text = err.detail;
            yield break;
        }

        if (request.responseCode >= 400)
        {
            errorText.text = "Ошибка сервера (" + request.responseCode + ")";
            Debug.LogError(request.downloadHandler.text);
            yield break;
        }
        if (request.responseCode==200){
            Debug.Log("YES");
            Debug.Log(request.downloadHandler.text);
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