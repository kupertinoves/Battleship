using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class AuthController : MonoBehaviour
{
    [Header("UI")]
    public TMP_InputField loginInput;
    public TMP_InputField passwordInput;
    public TMP_InputField emailInput; // может быть null для логина
    public string authUrl = "http://localhost:8000/register";
    public TMP_Text errorText;

    public void LoadNextScene()
    {
        Debug.Log("sceneName to load:Auth");
        SceneManager.LoadScene("Auth");
    }
    public void OnSubmit()
    {
        StartCoroutine(Register());
    }

    IEnumerator Register()
    {
        errorText.text = "";
        var data = new AuthRequest
        {
            login = loginInput.text,
            password = passwordInput.text,
            email = emailInput != null ? emailInput.text : null
        };

        string json = JsonUtility.ToJson(data);

        var request = new UnityWebRequest(authUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        Debug.Log(request.responseCode);

        if (request.responseCode == 409)
        {
            var err = JsonUtility.FromJson<RegisterErrorResponse>(request.downloadHandler.text);

            errorText.text = err.detail;
            yield break;
        }

        if (request.responseCode >= 400)
        {
            errorText.text = "Ошибка сервера (" + request.responseCode + ")";
            Debug.LogError(request.downloadHandler.text);
            yield break;
        }
        if (request.responseCode==201){
            LoadNextScene();
        }
    }
}

[System.Serializable]
public class AuthRequest
{
    public string login;
    public string password;
    public string email;
}
[System.Serializable]
public class RegisterErrorResponse
{
    public string detail;
}